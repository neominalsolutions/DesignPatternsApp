using System.Collections.Generic;

namespace DesignPatternsApp.Models
{

  /* 
   * 
    ---------------------------------------------------- BUILDER PATTERN -------------------------------------

    Karmaşık nesneleri adım adım oluşturmak için kullanılır. Bu desen, nesnenin oluşturulma sürecini dışarıdan yönetilebilir hale getirir ve esnek bir yapı sağlar.

    Bu şekilde kodun zincir halinde adım adım kullanıması yöntemine Fluent Interface diyoruz.

    StepBuilder, JobBuilder sınıfları buna bir örnektir.


    -----------------------------------------------------------------------------------------------------------


   -------------------------------------------- FACTORY METHDO PATTERN -----------------------------------------

  Factory Method tasarım deseni, nesne yaratma işlemini alt sınıflara devreden ve soyut bir sınıf üzerinden yapılmasını sağlayan bir desenidir.

    * Farklı Nesne Tipleri
    * Nesne Bağımlıklarını azaltma, IoC
    * İhtiyaca Göre Nesne Oluşturmak 
    
   JobBuilderFactory ve StepBuilderFactory -> Factory Method Design Pattern örneğidir.

  --------------------------------------------------------------------------------------------------------------

  ------------------------------------------- TEMPLATE METHOD PATTERN ------------------------------------------

  Bu tasarım deseni, bir algoritmanın iskeletini tanımlar ve alt sınıflara algoritmanın bazı adımlarını özelleştirme fırsatı verir.

  ItemReader,ItemWriter ve ItemProcess (CSV,XML) bazlı seçimine göre Execute methodu içerisdeki algoritma özelleşetirilebilir.  

  Step sınıfındaki Execute Methodu bu tasarım desenine bir örnektir.


  -------------------------------------------------------------------------------------------------------------

  ----------------------------------------------- COMMAND PATTERN --------------------------------------------------------------------

  Command Pattern sayesinde, bir işlemi (komutu) bir nesneye dönüştürürsünüz ve bu komut, gerektiğinde çalıştırılır. Bu da işlemleri daha esnek ve bağımsız hale getirir.


  Client: StepBuilder sınıfı. Burada komutları oluşturuyor ve adımı (Step) yapılandırıyorsunuz
  Invoker: Step, komutları çalıştıran sınıfı temsil eder. Invoker'ın görevi, komutları sırayla çalıştırmaktır.
  Receiver, komutları gerçekleştiren sınıfı temsil eder. (RetryCommand,SkipErrorCommand)
  Command:   ICommand arayüzü, komutları tanımlar.
  -----------------------------------------------------------------------------------------------------------------------------------

   * 
   * 
   */

  #region stepExecution


  public class StepExecution
  {
    public string StepName { get; set; }
    public BatchStatus Status { get; set; }

    public StepExecution(string stepName)
    {
      StepName = stepName;
      Status = BatchStatus.InProgress;
    }
  }

  public enum BatchStatus
  {
    Start,
    Complete,
    Failed,
    InProgress,
    Skipped
  }

  #endregion

  #region Step

  public interface IStep
  {
    string StepName { get; }

    void Execute(StepExecution stepExecution);
  }

  public class Step<TModel> : IStep
  {
    public string StepName { get; private set; }

    public IItemReader<TModel> ItemReader { get; private set; }
    public IItemProcessor<TModel> Processor { get; private set; }

    public IItemWriter<TModel> Writer { get; private set; }

    public int RetryCount { get; private set; }

    public bool IsFaultTolerant { get; private set; }

    public List<ICommand> Commands { get; private set; }



    public Step(string stepName, IItemReader<TModel> reader, IItemProcessor<TModel> processor, IItemWriter<TModel> writer, bool isfaultTolerant, List<ICommand> commands)
    {
      this.StepName = stepName;
      this.ItemReader = reader;
      this.Writer = writer;
      this.Processor = processor;
      this.Commands = commands;
      this.IsFaultTolerant = isfaultTolerant;

    }


    public void Execute(StepExecution stepExecution)
    {
      try
      {


        // İşlem başlatıldı
        stepExecution.Status = BatchStatus.InProgress;

        if (ItemReader != null)
        {

          // Okuma, işleme ve yazma döngüsüne başla
          List<TModel> items = ItemReader.Read(); // İlk okuma

          if (items != null && items.Any()) // Eğer item null değilse, işlem başla
          {
            try
            {
              items.ForEach(item =>
              {
                item = Processor.Process(item);
              });

              Writer.Write(items);
              stepExecution.Status = BatchStatus.Complete; // Başarılı bir şekilde tamamlandı
            }
            catch
            {
              stepExecution.Status = BatchStatus.Failed;
              // Hata durumunda Hata durum yönetim kodlarını çalıştır
              Commands.ForEach(c => c.Execute(stepExecution));
            }
          }
          else
          {
            stepExecution.Status = BatchStatus.Failed;  // Okuma işleminde veri yoksa hata ver
          }
        }
        else
        {
          stepExecution.Status = BatchStatus.Complete;
 
        }
      }
      catch (Exception ex)
      {
        if (IsFaultTolerant)
        {
          stepExecution.Status = BatchStatus.Failed;
          // Hata durumunda Hata durum yönetim kodlarını çalıştır
          Commands.ForEach(c => c.Execute(stepExecution));
        }

      }
    }
  }

  #endregion

  #region stepBuilder


  public interface IStepBuilder<TModel>
  {
    IStep Build();
    IStepBuilder<TModel> Reader(IItemReader<TModel> itemReader);
    IStepBuilder<TModel> Processor(IItemProcessor<TModel> itemProcessor);
    IStepBuilder<TModel> Writer(IItemWriter<TModel> itemWriter);

    IStepBuilder<TModel> Retry(int retryCount); // hata durumunda kaç kez tekrarlı işlem yapacağı.
    IStepBuilder<TModel> SkipError<ExceptionClass>() where ExceptionClass : Exception; // Kaç adet hataya kadar hatalı kodu atlatacağı

    IStepBuilder<TModel> FaultTolerant();
  }


  public class StepBuilder<TModel> : IStepBuilder<TModel>
  {
    private string _stepName;
    private IItemProcessor<TModel> _itemProcessor;
    private IItemReader<TModel> _itemReader;
    private IItemWriter<TModel> _itemWriter;
    private Exception skipException;
    private bool isFaultTolerant;
    private List<ICommand> _commands = new List<ICommand>();


    public StepBuilder(string stepName)
    {
      _stepName = stepName;
    }

    public IStep Build()
    {
     

      return new Step<TModel>(_stepName, _itemReader, _itemProcessor, _itemWriter,isFaultTolerant, _commands);
    }

    public IStepBuilder<TModel> FaultTolerant()
    {
      isFaultTolerant = true;
      return this;
    }

    public IStepBuilder<TModel> Processor(IItemProcessor<TModel> itemProcessor) 
    {
      _itemProcessor = itemProcessor;
      return this;
    }

    public IStepBuilder<TModel> Reader(IItemReader<TModel> itemReader)
    {
      _itemReader = itemReader;
      return this;
    }

    public IStepBuilder<TModel> Retry(int retryCount)
    {
      if (isFaultTolerant)
        _commands.Add(new RetryCommand(retryCount));
      
      return this;
    }


    public IStepBuilder<TModel> SkipError<ExceptionClass>() where ExceptionClass : Exception
    {
      if (isFaultTolerant)
        _commands.Add(new SkipErrorCommand<ExceptionClass>());
      
      
      return this;
      
    }

    public IStepBuilder<TModel> Writer(IItemWriter<TModel> itemWriter)
    {
      _itemWriter = itemWriter;
      return this;
    }
  }

  #endregion

  #region stepBuilderFactory

  public interface IStepBuilderFactory<TModel>
  {
    IStepBuilder<TModel> CreateStep(string stepName);

  }

  public class SimpleStepBuilderFactory<TModel> : IStepBuilderFactory<TModel>
  {
    public IStepBuilder<TModel> CreateStep(string stepName)
    {
      return new StepBuilder<TModel>(stepName);
    }
  }

  #endregion

  #region CommandPattern

  // COMMAND PATTERN START

  public interface ICommand
  {
    void Execute(StepExecution stepExecution);
  }

  public class RetryCommand : ICommand
  {
    private readonly int _maxRetries;
    private int _currentRetry = 0;

    public RetryCommand(int retryCount)
    {
      _maxRetries = retryCount;
    }

    public void Execute(StepExecution stepExecution)
    {
      // Retry mekanizması
      while (_currentRetry < _maxRetries)
      {
        try
        {
          Console.WriteLine($"Tekrar deneme {_currentRetry + 1}/{_maxRetries}");
          // Burada işlem yapılır ve başarılı olursa döngü sonlanır
          stepExecution.Status = BatchStatus.Complete;
          return;
        }
        catch (Exception ex)
        {
          Console.WriteLine($"Hata: {ex.Message}");
          _currentRetry++;
          if (_currentRetry >= _maxRetries)
          {
            stepExecution.Status = BatchStatus.Failed;
          }
        }
      }
    }
  }

  public class SkipErrorCommand<TException> : ICommand where TException:class
  {
   

    public void Execute(StepExecution stepExecution)
    {
      try
      {
        // Eğer işlemde hata varsa atla
        if (stepExecution.Status == BatchStatus.Failed)
        {
          Console.WriteLine("Bir hata oluştu, step atlatıyoruz " + stepExecution.StepName);
          stepExecution.Status = BatchStatus.Skipped;  // Atlatma durumuna geç
        }
        else
        {
          // Eğer hata yoksa, normal işlemi devam ettir
          Console.WriteLine("İşlem başarılı, devam ediyor...");
          stepExecution.Status = BatchStatus.Complete;
        }
      }
      catch (Exception ex)
      {
        // Hata olursa da atlatma yapılır
        Console.WriteLine($"Skip Command Hata: {ex.Message}");
        stepExecution.Status = BatchStatus.Skipped;
      }
    }
  }

  // COMMAND PATTERN END

  #endregion
}
