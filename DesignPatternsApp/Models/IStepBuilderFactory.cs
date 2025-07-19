using System.Collections.Generic;

namespace DesignPatternsApp.Models
{

  // StepBuilder -> Builder Design Pattern ->  Karmaşık nesneleri adım adım oluşturmak için kullanılır. Bu desen, nesnenin oluşturulma sürecini dışarıdan yönetilebilir hale getirir ve esnek bir yapı sağlar.


  // Fluent Interface: Kodunuzu daha okunabilir ve anlaşılır kılar. Zincirleme metodlar kullanılarak adım adım nesne oluşturulabilir.


  // SimpleStepBuilderFactory ->  Factory Method Design Pattern genellikle belirli bir tek bir türde nesne oluşturmak için kullanılan basit bir yapıdır. Bu desen, nesneleri belirli bir fabrika sınıfı veya fabrika metoduyla yaratmayı sağlar.

  // Template Method deseni, bir algoritmanın iskeletini tanımlar ve alt sınıflara algoritmanın bazı adımlarını özelleştirme fırsatı verir.Step.Execute metodu bu desenin bir örneğidir. ItemReader,ItemWriter ve Process seçimine göre Execute methodu içerisindeki algoritma özelleştirilmiştir.

  public enum BatchStatus
  {
    Start,
    Complete,
    Failed,
    InProgress
  }

  public class StepExecution
  {
    public string StepName { get; set; }
    public BatchStatus Status { get; set; }
    //public ExecutionContext ExecutionContext { get; set; }

    public StepExecution(string stepName)
    {
      StepName = stepName;
      Status = BatchStatus.InProgress;
      //ExecutionContext = new ExecutionContext();  // Her adım için ExecutionContext başlatıyoruz
    }
  }

  public class Step<TModel> : IStep
  {
    public string StepName { get; private set; }



    public IItemReader<TModel> ItemReader { get; private set; }
    public IItemProcessor<TModel> Processor { get; private set; }

    public IItemWriter<TModel> Writer { get; private set; }

    public Step(string stepName, IItemReader<TModel> reader, IItemProcessor<TModel> processor, IItemWriter<TModel> writer)
    {
      this.StepName = stepName;
      this.ItemReader = reader;
      this.Writer = writer;
      this.Processor = processor;
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
              stepExecution.Status = BatchStatus.Failed; // İşlem sırasında hata oldu
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
        // Hata oluşursa, adımı başarısız olarak işaretle ve hata yönetimini yap
        Console.WriteLine($"Error during step execution: {ex.Message}");
        stepExecution.Status = BatchStatus.Failed;
      }
    }
  }

  public interface IStep
  {
    string StepName { get; }

    void Execute(StepExecution stepExecution);
  }

  public interface IStepBuilder<TModel>
  {
    IStep Build();
    IStepBuilder<TModel> Reader(IItemReader<TModel> itemReader);
    IStepBuilder<TModel> Processor(IItemProcessor<TModel> itemProcessor);
    IStepBuilder<TModel> Writer(IItemWriter<TModel> itemWriter);
  }
 
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


  public class StepBuilder<TModel> : IStepBuilder<TModel>
  {
    private string _stepName;
    private IItemProcessor<TModel> _itemProcessor;
    private IItemReader<TModel> _itemReader;
    private IItemWriter<TModel> _itemWriter;

    public StepBuilder(string stepName)
    {
      _stepName = stepName;
    }

    public IStep Build()
    {
      return new Step<TModel>(_stepName, _itemReader, _itemProcessor, _itemWriter);
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

    public IStepBuilder<TModel> Writer(IItemWriter<TModel> itemWriter)
    {
      _itemWriter = itemWriter;
      return this;
    }
  }
}
