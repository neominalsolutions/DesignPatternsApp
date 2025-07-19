using System.Collections.Immutable;
using System.Reflection.Metadata;
using static DesignPatternsApp.Models.JobParameters;

namespace DesignPatternsApp.Models
{


  /*
   *  Composite Design Pattern'i bir nesne ağacı yapısı kurarak bileşenlerin birbirleriyle ilişkisini yönetmek için kullanırız.    Burada, Job sınıfı, birden fazla IStep nesnesi içeriyor ve AddStep metodu her yeni adımı bu koleksiyona ekliyor. Bu, bir Composite ilişkisi kurar çünkü Job tekil adımları (steps) ve gruplarını aynı şekilde yönetiyor.

      job.AddStep(userStep);
      job.AddStep(personStep);


  CsvReader,CsvWriter, XmlReader, XmlWriter,SimpleItemProcessor ->  Strategy deseni, belirli bir işlevi bir dizi algoritma ile değiştirebilmenizi sağlar. Bir algoritmayı (veya işlem sırasını) sınıf dışındaki nesnelerde değiştirebilirsiniz. CsvReader XmlReader sınıflarındaki Read Write işlemleri Strategy tasarım desenine girer.

    JobLauncher -> Singleton tasarım deseni, sınıfın yalnızca bir örneğini oluşturmak ve bu örneği global olarak erişilebilir yapmak için kullanılır. Bunun için sınıfın kendi içinde özel bir statik nesne bulunur ve bu nesne yalnızca bir kez oluşturulur.


  BatchStatus -> State Pattern
  
  State Design Pattern, bir nesnenin iç durumuna bağlı olarak farklı davranışlar sergilemesini sağlar

  Context (JobExecution): Durum bilgisini tutar ve durum değiştikçe ilgili durumu (State) günceller.

  State (BatchStatus): Bir iş akışının farklı durumlarını temsil eder. Örneğin, Başlatıldı, Devam Ediyor, Tamamlandı, Başarısız gibi.

  ConcreteState (CompletedState, FailedState, InProgressState): State'in somut sınıfları, her biri kendi başına belirli bir durumu temsil eder.

  Job'a ait State durumlarını JobExecution sınıfı yönettiği için state değişimlerini JobExection sınıfı içerisinde yaptık


  JobLoggerListener ->  Observer Tasarım Deseni (Observer Design Pattern), bir nesnenin durumu değiştiğinde, bu nesneye bağlı olan tüm diğer nesnelerin otomatik olarak bilgilendirildiği bir tasarım desenidir.

   * 
   */

  public interface IJobBuilder
  {
    IJob Build();
    IJobBuilder Start(IStep step);

    IJobBuilder Next(IStep step);

    IJobBuilder AddListener(IJobListener listener);

  }

  public class JobBuilder : IJobBuilder
  {

    private IJob _job;

    public JobBuilder(string jobName)
    {
      _job = new Job(jobName);
    }

    public IJobBuilder AddListener(IJobListener listener)
    {
      _job.AddListener(listener);
      return this;
    }

    public IJob Build()
    {
      return _job;
    }

    public IJobBuilder Next(IStep step)
    {
      if (this._job.GetSteps().Any())
      {
        this._job.AddStep(step);
        return this;
      }
      else
      {
        throw new ArgumentException("Start Step Bulunamadı");
      }
      
    }

    public IJobBuilder Start(IStep step)
    {
      this._job.AddStep(step);
      return this;
    }


  }
  public interface IJobBuilderFactory
  {
    IJobBuilder CreateJob(string jobName);

  }

  public class SimpleJobBuilderFactory : IJobBuilderFactory
  {
    public IJobBuilder CreateJob(string jobName)
    {
      return new JobBuilder(jobName);
    }
  }


  public interface IJob
  {
    string JobName { get; }
    JobExecution Execute(JobParameters parameters);

    public IImmutableList<IStep> GetSteps();
    void AddStep(IStep step);

    void AddListener(IJobListener listener);

    void RemoveListener(IJobListener listener);
  }

  public class Job : IJob
  {
    public string JobName { get; private set; }


    private List<IStep> _steps = new List<IStep>();

    private List<IJobListener> _listeners = new List<IJobListener>();  // Observer'ları tutacak liste


    public IImmutableList<IStep> GetSteps()
    {
      return _steps.ToImmutableList();
    }

    public Job(string jobName)
    {
      JobName = jobName;
     
    }

    public JobExecution Execute(JobParameters parameters)
    {

    
      var jobExecution = new JobExecution(this, parameters);
      NotifyBeforeJob(jobExecution);  // İşlem başlamadan önce dinleyicilere haber ver
      jobExecution.Start(); // InProgress State
 

      try
      {
        foreach (var step in _steps)
        {

          var stepExecution = new StepExecution(step.StepName);
          step.Execute(stepExecution);
          jobExecution.AddStepExecution(stepExecution);

        }

        jobExecution.Complete(); // Complete State

      }
      catch (Exception)
      {
        jobExecution.Fail(); // Fail State
        NotifyOnJobFailure(jobExecution);  // İşlemde hata olunca dinleyicilere haber ver
      }

      NotifyAfterJob(jobExecution);  // İşlem tamamlandığında dinleyicilere haber ver


      // İşlem tamamlandığında dinleyiciyi otomatik olarak kaldırıyoruz
      foreach (var listener in _listeners.ToList())
      {
        RemoveListener(listener);  // Her dinleyiciyi kaldıralım
      }

      return jobExecution;
    }

    public void AddStep(IStep step)
    {
      _steps.Add(step);
    }

    // OBSERVER PATTERN
    public void AddListener(IJobListener listener)
    {
      _listeners.Add(listener);  // Observer ekleme
    }

    public void RemoveListener(IJobListener listener)
    {
      _listeners.Remove(listener);  // Observer çıkarma
    }

    private void NotifyBeforeJob(JobExecution jobExecution)
    {
      foreach (var listener in _listeners)
      {
        listener.BeforeJob(jobExecution);
      }
    }

    // Job tamamlandığında tüm dinleyicilere haber ver
    private void NotifyAfterJob(JobExecution jobExecution)
    {
      foreach (var listener in _listeners)
      {
        listener.AfterJob(jobExecution);
       
      }
    }

    // Job başarısız olduğunda tüm dinleyicilere haber ver
    private void NotifyOnJobFailure(JobExecution jobExecution)
    {
      foreach (var listener in _listeners)
      {
        listener.OnJobFailure(jobExecution);
      }
    }
  }

  public class JobExecution
  {
    public Job Job { get; private set; }

    public IJobState JobState { get; private set; }
    public BatchStatus Status { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public JobParameters JobParameters { get; private set; }
    public List<StepExecution> StepExecutions { get; private set; }

    public JobExecution(Job job, JobParameters jobParameters)
    {
      Job = job;
      JobParameters = jobParameters;
      StepExecutions = new List<StepExecution>();
    }

    public void AddStepExecution(StepExecution stepExecution)
    {
      StepExecutions.Add(stepExecution);
    }


    public void Start()
    {
      JobState = new InProgressState();
      JobState.Handle(this);  // Durumu işliyoruz
    }

    public void Complete()
    {
      JobState = new CompletedState();
      JobState.Handle(this);  // Durumu işliyoruz
    }

    public void Fail()
    {
      JobState = new FailedState();
      JobState.Handle(this);  // Durumu işliyoruz
    }
  }

  public class JobParameters
  {
    public Dictionary<string, string> Parameters { get; set; }

    public JobParameters()
    {
      Parameters = new Dictionary<string, string>();
    }

    public void AddParameter(string key, string value)
    {
      Parameters[key] = value;
    }

    public string GetParameter(string key)
    {
      return Parameters.ContainsKey(key) ? Parameters[key] : null;
    }

    public interface IJobLauncher
    {
      JobExecution Run(IJob job, JobParameters parameters);
    }

    public class JobLauncher : IJobLauncher
    {
      // Tek bir örnek (instance) tutan özel bir statik alan
      private static JobLauncher _instance;
      private static readonly object _lock = new object();

      // Singleton'dan bir örnek almak için kullanılan public property
      public static JobLauncher Instance
      {
        get
        {
          lock (_lock)
          {
            if (_instance == null)
            {
              _instance = new JobLauncher(); // Yalnızca ilk erişimde örnek yaratılır
            }
            return _instance;
          }
        }
      }

      // Singleton'dan dışarıdan örnek alınmasını engellemek için constructor private
      private JobLauncher() { }

      // Job'u çalıştırmak için kullanılan metot
      public JobExecution Run(IJob job, JobParameters parameters)
      {
        Console.WriteLine($"Job '{job.JobName}' started.");
        return job.Execute(parameters);
      }
    }
  }

  /* STATE DESING PATTERN */
  public interface IJobState
  {
    void Handle(JobExecution jobExecution);
  }

  // Concrete States(Durumlar)

  public class InProgressState : IJobState
  {
    public void Handle(JobExecution jobExecution)
    {
      Console.WriteLine("Job is in progress.");
      // İşlem devam ediyor
      jobExecution.Status = BatchStatus.InProgress;
      jobExecution.StartTime = DateTime.Now;
    }
  }

  public class CompletedState : IJobState
  {
    public void Handle(JobExecution jobExecution)
    {
      Console.WriteLine("Job is completed.");
      // İşlem tamamlandı
     
      jobExecution.Status = BatchStatus.Complete;
      jobExecution.EndTime = DateTime.Now;
    }
  }

  public class FailedState : IJobState
  {
    // Context State güncelliyoruz.
    public void Handle(JobExecution jobExecution)
    {
      Console.WriteLine("Job is failed.");
      // İşlem başarısız oldu
      jobExecution.Status = BatchStatus.Failed;
      jobExecution.EndTime = DateTime.Now;

    }
  }

  // OBSERVABLE PATTERN

  public interface IJobListener
  {
    void BeforeJob(JobExecution jobExecution);
    void AfterJob(JobExecution jobExecution);
    void OnJobFailure(JobExecution jobExecution);
  }

  // JobListener  
  public class JobLoggerListener : IJobListener
  {
    public void AfterJob(JobExecution jobExecution)
    {
      if (jobExecution.Status == BatchStatus.Complete)
      {
        Console.WriteLine($"Job '{jobExecution.Job.JobName}' completed successfully.");
      }
      else if (jobExecution.Status == BatchStatus.Failed)
      {
        Console.WriteLine($"Job '{jobExecution.Job.JobName}' failed.");
      }
    }

    public void BeforeJob(JobExecution jobExecution)
    {
      Console.WriteLine($"Job '{jobExecution.Job.JobName}' is starting...");
    }

    public void OnJobFailure(JobExecution jobExecution)
    {
      Console.WriteLine($"Job '{jobExecution.Job.JobName}' has failed and requires attention.");
    }
  }




}
