using DesignPatternsApp.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CSharp.RuntimeBinder;
using Microsoft.OpenApi.Expressions;
using static DesignPatternsApp.Models.JobParameters;

namespace DesignPatternsApp.Controllers
{
  [Route("api/[controller]")]
  [ApiController]
  public class BatchsController : ControllerBase
  {
    public class User
    {
      public string UserName { get; set; }
      public string Email { get; set; }

    }

    public class Person
    {
      public string Name { get; set; }
      public string LastName { get; set; }

      public int Age { get; set; }
    }

    public class UserItemProcessor : IItemProcessor<User>
    {
      public User Process(User item)
      {
        item.Email = item.Email.ToUpper();
        item.UserName = item.UserName.ToUpper();

        return item;
      }
    }

    public class PersonItemProcessor : IItemProcessor<Person>
    {
      public Person Process(Person item)
      {
        item.Age = item.Age < 18 ? -1 : item.Age;
        item.Name = item.Name.ToLower();
        item.LastName = item.LastName.ToLower();

        return item;
      }
    }


    // Colleagues -> Mediator üzerinden iletişim kuran nesnelerdir. UserJobRequest, UserJobExecutorHandler
    public class UserJobRequest : IJobRequest
    {
      public string JobName { get; set; }
      public Dictionary<string, string> Parameters { get; set; }

      public UserJobRequest(string jobName, Dictionary<string,string> parameters)
      {
        JobName = jobName;
        Parameters = parameters;
      }
    }

    // Concrete Mediator
    public class UserJobExecutorHandler : IJobExecutorHandler
    {
      public void Execute(IJobRequest jobRequest)
      {

        var reader = new CsvItemReader<User>("users.csv");
        var readerProxy = new CsvItemReaderProxy<User>(reader); // Okuma öncesi dosya kontrolleri yapılır.


        // Decorator Design Pattern sayesinde davranışı değiştirmeden processor sınıflarına yeni özellikler kazandırdık.
        var processor = new UserItemProcessor();
        var processor01 = new NumericFieldItemDecorator<User>(processor);
        var processor02 = new StringFieldItemDecorator<User>(processor01);

        var writer = new XMLItemWriter<User>("users.xml");


        IStepBuilderFactory<User> stepBuilderFactory1 = new SimpleStepBuilderFactory<User>();
        IStep userStep = stepBuilderFactory1
          .CreateStep("userStep")
          .Reader(readerProxy) // Proxy üzerinden read eder.
          .Processor(processor02)
          .Writer(writer)
          .Build();

        // ExternalCsvItemAdapter kullanımı
        IStepBuilderFactory<Person> stepBuilderFactory2 = new SimpleStepBuilderFactory<Person>();
        // ana yapımızı dışarıdan kullanılan bir adapter sınıfı ile çalıştırdık genel yapımızı bozmadan
        var reader1 = new ExternalCsvItemReaderAdapter<Person>("persons.csv");
        var processor1 = new PersonItemProcessor();
        var writer1 = new XMLItemWriter<Person>("persons.xml");

        // Fault Tolerant Step
        IStep personStep = stepBuilderFactory2
          .CreateStep("personStep")
          .Reader(reader1)
          .Processor(processor1)
          .Writer(writer1)
          .FaultTolerant()
          .Retry(3) // 3 Errors
          .SkipError<RuntimeBinderException>()
          .Build();

        var jobListener = new JobLoggerListener();


        IJobBuilderFactory jobBuilderFactory = new SimpleJobBuilderFactory();
        IJob job = jobBuilderFactory
          .CreateJob(jobRequest.JobName)
          .Start(userStep)
          .Next(personStep)
          .AddListener(jobListener)
          .Build();

        JobParameters jobParameters = new JobParameters();

        foreach (KeyValuePair<string,string> item in jobRequest.Parameters)
        {
          jobParameters.AddParameter(item.Key, item.Value);
        }

        JobLauncher.Instance.Run(job, jobParameters);

      }
    }


    [HttpPost("executeUserJob")]
    public IActionResult UserJob([FromBody] UserJobRequest request)
    {
      // JobMediator -> Concrete Mediator: Mediator interface'ini uygulayan somut sınıf. 
      // Her job tipi, farklı bir işleyici (handler) üzerinden çalıştırılır.
      var jobMediator = new JobMediator();
      // Job Request ve Handler'ın Register Edilmesi:
      jobMediator.RegisterJobHandler(request.JobName, new UserJobExecutorHandler());
      jobMediator.ExecuteJob(request);
    

      return Ok();
    }
  }
}
