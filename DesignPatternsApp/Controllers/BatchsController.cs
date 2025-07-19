using DesignPatternsApp.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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


   


    [HttpPost]
    public IActionResult test()
    {

      var reader = new CsvItemReader<User>("users.csv");
      var readerProxy = new CsvItemReaderProxy<User>(reader, "users.csv"); // Okuma öncesi dosya kontrolleri yapılır.


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

      IStepBuilderFactory<Person> stepBuilderFactory2 = new SimpleStepBuilderFactory<Person>();
      var reader1 = new CsvItemReader<Person>("persons.csv");
      var processor1 = new PersonItemProcessor();
      var writer1 = new XMLItemWriter<Person>("persons.xml");

      IStep personStep = stepBuilderFactory2
        .CreateStep("personStep")
        .Reader(reader1)
        .Processor(processor1)
        .Writer(writer1)
        .Build();

      var jobListener = new JobLoggerListener();


      IJobBuilderFactory jobBuilderFactory = new SimpleJobBuilderFactory();
      IJob job = jobBuilderFactory
        .CreateJob("userJob")
        .Start(userStep)
        .Next(personStep)
        .AddListener(jobListener)
        .Build();


      JobParameters jobParameters = new JobParameters();
      jobParameters.AddParameter("JobId", Guid.NewGuid().ToString());
      JobLauncher.Instance.Run(job, jobParameters);


     



      return Ok();
    }
  }
}
