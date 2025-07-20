using DesignPatternsApp.Models;
using System.Net;
using System.Reflection;
using System.Xml;

namespace DesignPatternsApp.Models
{
  // Abstract Factory deseninin temeli olan farklı türdeki nesnelerin aynı arayüzle üretilmesini sağlıyor.

  /*
   * 
   
  ---------------------------------------------- ABSTRACT FACTORY ------------------------------------------

  Abstract Factory Pattern, benzer nesne aileleri oluşturmak için kullanılan bir tasarım desenidir. Bu desen, her nesne ailesinin farklı türdeki nesnelerini aynı arayüz üzerinden yaratmanıza olanak tanır. Yani, nesnelerin somut türlerini istemci sınıfına gizler ve istemcinin yalnızca arayüzle çalışmasını sağlar.

  Abstract Factory: Nesne oluşturma yöntemlerini tanımlar IBatchProcessingFactory.

  Concrete Factory: Abstract Factory arayüzünü implement eder ve somut nesneleri oluşturur. CsvProcessingFactory, XMLProcessinFactory

  Abstract Product: IItemReader<TModel>, IItemWriter<TModel>, ItemProcessor<TModel>

  Concrete Product: Somut sınıflardır, CsvItemReader, CsvItemWriter, XMLItemReader, XMLItemWriter


  ---------------------------------------------------------------------------------------------------------
   
  ----------------------------------------------- DECORATOR PATTERN ------------------------------------------
   
   Decorator Design Pattern, mevcut bir sınıfın işlevselliğini değiştirmeden ona yeni işlevsellik eklememizi sağlayan bir yapıdır. Bu tasarım deseni, genellikle bir sınıfın işlevlerini dinamik olarak değiştirmemizi sağlar.

   ItemProcessorDecorator -> (StringFieldItemDecorator,NumericFieldItemDecorator)
  
  ---------------------------------------------------------------------------------------------------------- 

  ------------------------------------------------ PROXY PATTERN ---------------------------------------------
  Proxy, gerçek nesneye olan erişimi kontrol etmek, sınırlamak, izlemek ya da optimize etmek amacıyla kullanılır. Gerçek nesneye doğrudan erişim yerine, proxy nesnesi kullanılarak arayüz sunulur.

  Lazy Loading, Erişim Kontrolü,  Performans İyileştirme, Ağ İletişimi, Güvenlik gibi senaryolarda tercih edilir.

  Aşağıdaki örnek de Erişim Kontrolü'ne bir örnek verilmiştir.

  CsvItemReaderProxy -> Real: CsvItemReader

  -------------------------------------------------------------------------------------------------------------


  ------------------------------------------------- ADAPTER PATTERN ---------------------------------------------

    Adapter Pattern (Adaptör Tasarım Deseni), yazılım geliştirmede, uyumsuz iki arayüzün birlikte çalışabilmesini sağlamak amacıyla kullanılan bir yapısal tasarım desenidir. 

     ExternalCsvItemReader -> ItemReader Adaptörü üzerinden Sistemimizde çalışılabilir bir şekilde adapte ediliyor.

  --------------------------------------------------------------------------------------------------------------


  --------------------------------------------------- MEDIATOR -------------------------------------------------

   Mediator Pattern (Aracı Deseni), yazılım tasarımında, birden fazla nesnenin birbirleriyle doğrudan iletişim kurmak yerine, birbirlerine aracı (Mediator) üzerinden iletişim kurmasını sağlayan bir yapıdır.  Bu desende, aracı nesne tüm iletişimi yönetir, böylece nesneler birbirlerine doğrudan bağımlı olmazlar. Bu, yazılımın daha esnek, bakımı kolay ve test edilebilir olmasını sağlar.

  JobRequest, JobExecutor (Colleagues -> Mediator üzerinden iletişim kuran nesnelerdir. UserJobRequest, UserJobExecutorHandler)

  JobMediator -> Concrete Mediator Colleagues nesneleri arasındaki iletişimi saylayan yöneten sınıf

  -------------------------------------------------------------------------------------------------------------

   */


  // ABSTRACT FACTORY START

  #region BatchProcessingAbstractFactory

  public interface IItemReader<TModel>
  {
    List<TModel> Read();
  }

  public interface IItemWriter<TModel>
  {
    void Write(List<TModel> items);
  }

  public interface IItemProcessor<TModel>
  {
    TModel Process(TModel item);
  }

  public interface IBatchProcessingFactory<TModel>
  {
    IItemReader<TModel> CreateReader();
    IItemWriter<TModel> CreateWriter();

    IItemProcessor<TModel> CreateProcessor();
  }

  public class CsvProcessingFactory<TModel> : IBatchProcessingFactory<TModel>
  {
    private string filename;

    public CsvProcessingFactory(string filename)
    {
      this.filename = filename;
    }

    public IItemProcessor<TModel> CreateProcessor()
    {
      return new SimpleItemProcessor<TModel>();
    }

    public IItemReader<TModel> CreateReader()
    {
      return new CsvItemReader<TModel>(this.filename);
    }

    public IItemWriter<TModel> CreateWriter()
    {
      return new CsvItemWriter<TModel>(this.filename);
    }
  }

  public class XMLProcessingFactory<TModel> : IBatchProcessingFactory<TModel>
  {
    private string filename;

    public XMLProcessingFactory(string filename)
    {
      this.filename = filename;
    }

    public IItemProcessor<TModel> CreateProcessor()
    {
      return new SimpleItemProcessor<TModel>();
    }

    public IItemReader<TModel> CreateReader()
    {
      return new XMLItemReader<TModel>(this.filename);
    }

    public IItemWriter<TModel> CreateWriter()
    {
      return new XMLItemWriter<TModel>(this.filename);
    }
  }

  public class CsvItemReader<TModel> : IItemReader<TModel>
  {
    private string _fileName;

    public CsvItemReader(string fileName)
    {
      ArgumentNullException.ThrowIfNull(fileName, $"{nameof(CsvItemReader<TModel>)} fileName is Null");
      _fileName = fileName;
    }

    public string FileName => _fileName;

    public List<TModel> Read()
    {
      var result = new List<TModel>();

      var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _fileName);

      try
      {
        using (StreamReader reader = new StreamReader(filePath))
        {
          // Başlık satırını oku (ilk satır genellikle başlık olur)
          string header = reader.ReadLine();
          var properties = typeof(TModel).GetProperties(); // Reflection ile sınıfın özelliklerini al

          string line;
          while ((line = reader.ReadLine()) != null)
          {
            var values = line.Split(',');

            // Yeni nesne oluştur
            TModel instance = Activator.CreateInstance<TModel>();

            // Reflection ile her özelliğe değer ata
            for (int i = 0; i < values.Length; i++)
            {
              var property = properties[i];
              if (property.CanWrite)
              {
                var value = values[i];
                if (property.PropertyType == typeof(string))
                {
                  property.SetValue(instance, value); // String değerleri doğrudan ata
                }
                else if (property.PropertyType == typeof(int))
                {
                  if (int.TryParse(value, out int intValue))
                  {
                    property.SetValue(instance, intValue); // int değerleri dönüştürerek ata
                  }
                }
                // Diğer veri türleri için dönüşümler yapılabilir (DateTime, bool, vs.)
              }
            }

            result.Add(instance);
          }
        }

        Console.WriteLine("CSV dosya okuma işlemi başarılı!");
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Hata oluştu: {ex.Message}");
      }

      return result;

    }
  }

  public class CsvItemWriter<TModel> : IItemWriter<TModel>
  {
    private string _fileName;
    public CsvItemWriter(string fileName)
    {
      ArgumentNullException.ThrowIfNull(fileName, $"{nameof(CsvItemWriter<TModel>)} fileName is Null");
      _fileName = fileName;
    }

    public void Write(List<TModel> items)
    {
      var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _fileName);



      // Dosya yazma işlemi
      try
      {
        using (StreamWriter writer = new StreamWriter(filePath))
        {
          if (items != null && items.Count > 0)
          {
            // İlk item'ın property'lerinden başlık satırı yazalım
            var properties = typeof(object).GetProperties();

            // Başlıkları yazalım
            writer.WriteLine(string.Join(",", properties.Select(p => p.Name)));

            // Her item'ı yazalım
            foreach (var item in items)
            {
              var values = new List<string>();
              foreach (var property in properties)
              {
                // Özellik değerini alalım
                var value = property.GetValue(item)?.ToString() ?? string.Empty;
                values.Add(value);
              }

              // Özellik değerlerini CSV formatında yazalım
              writer.WriteLine(string.Join(",", values));
            }

            Console.WriteLine("Csv dosya yazma işlemi başarılı.");
          }
          else
          {
            Console.WriteLine("Liste boş, yazma işlemi yapılmadı.");
          }
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Hata oluştu: {ex.Message}");
      }
    }
  }


  public class XMLItemReader<TModel> : IItemReader<TModel>
  {
    private string _fileName;
    public XMLItemReader(string fileName)
    {
      ArgumentNullException.ThrowIfNull(fileName, $"{nameof(XMLItemReader<TModel>)} fileName is Null");
      _fileName = fileName;
    }

    public List<TModel> Read()
    {
      List<TModel> items = new List<TModel>();

      try
      {

        var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _fileName);

        if (!Path.Exists(filePath))
          throw new FileNotFoundException();

        using (XmlReader reader = XmlReader.Create(_fileName))
        {
          // XML dosyasını okuyoruz
          TModel currentItem = default;
          PropertyInfo[] properties = null;

          while (reader.Read())
          {
            // Element başında, bir item başlangıcını bulduk
            if (reader.NodeType == XmlNodeType.Element)
            {
              // Yeni bir item başlatıyoruz (örneğin <User>)
              if (currentItem == null || reader.Name != typeof(TModel).Name)
              {
                // TModel türünden bir nesne oluşturuyoruz
                currentItem = Activator.CreateInstance<TModel>();
                properties = typeof(object).GetProperties();
              }

              // Özellik adını ve değerini alıyoruz
              string propertyName = reader.Name;

              if (reader.Read() && !string.IsNullOrEmpty(reader.Value))
              {
                // Özelliğin değerini set ediyoruz
                PropertyInfo property = Array.Find(properties, p => p.Name == propertyName);
                if (property != null)
                {
                  var value = Convert.ChangeType(reader.Value, property.PropertyType);
                  property.SetValue(currentItem, value);
                }
              }
            }
            // Eğer öğe kapatılıyorsa, item'ı listeye ekliyoruz
            else if (reader.NodeType == XmlNodeType.EndElement && reader.Name == typeof(TModel).Name)
            {
              items.Add(currentItem);
              currentItem = default;  // Sonraki öğeye geçiyoruz
            }
          }
        }

        Console.WriteLine("XML dosya okuma işlemi başarılı.");
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Hata oluştu: {ex.Message}");
      }

      return items;
    }
  }

  public class SimpleItemProcessor<TModel> : IItemProcessor<TModel>
  {
    public TModel Process(TModel item)
    {
      throw new NotImplementedException();
    }
  }


  public class XMLItemWriter<TModel> : IItemWriter<TModel>
  {
    private string _fileName;
    public XMLItemWriter(string fileName)
    {
      ArgumentNullException.ThrowIfNull(fileName, $"{nameof(XMLItemWriter<TModel>)} fileName is Null");
      _fileName = fileName;
    }

    public void Write(List<TModel> items)
    {
      // Dosyaya yazma işlemi için XMLWriter kullanacağız
      try
      {
        var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _fileName);


        using (XmlWriter writer = XmlWriter.Create(filePath))
        {
          writer.WriteStartDocument(); // XML belgesinin başlangıcı

          writer.WriteStartElement(typeof(TModel).Name + "s"); // Root element (örneğin <Users>)

          foreach (var item in items)
          {
            // Her item için bir element açıyoruz
            writer.WriteStartElement(typeof(TModel).Name);

            // Reflection ile nesnenin özelliklerini alıyoruz
            var properties = typeof(TModel).GetProperties();

            foreach (var property in properties)
            {
              // Her bir özelliğin adını ve değerini alıyoruz
              var value = property.GetValue(item)?.ToString() ?? string.Empty;

              // Özelliği XML element olarak yazıyoruz
              writer.WriteElementString(property.Name, value);
            }

            // Item'ı kapatıyoruz
            writer.WriteEndElement();
          }

          writer.WriteEndElement(); // Root elementi kapatıyoruz
          writer.WriteEndDocument(); // XML belgesinin sonu

          Console.WriteLine("XML dosya yazma işlemi başarılı.");
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Hata oluştu: {ex.Message}");
      }
    }
  }


  // ABSTRACT FACTORY END
  #endregion

  #region DecoratorPattern


  // DECORATOR PATTERNS START

  public abstract class ItemProcessorDecorator<TModel> : IItemProcessor<TModel>
  {
    protected IItemProcessor<TModel> _decoratedProcessor;

    public ItemProcessorDecorator(IItemProcessor<TModel> decoratedProcessor)
    {
      _decoratedProcessor = decoratedProcessor;
    }

    public virtual TModel Process(TModel item)
    {
      return _decoratedProcessor.Process(item);  // Temel işleme devam et
    }
  }

  // Nesne içerisindeki String alanların validasyonlarını yapar.
  public class StringFieldItemDecorator<TModel> : IItemProcessor<TModel>
  {
    private readonly IItemProcessor<TModel> _decoratedProcessor;

    public StringFieldItemDecorator(IItemProcessor<TModel> decoratedProcessor)
    {
      _decoratedProcessor = decoratedProcessor;
    }

    public TModel Process(TModel item)
    {
      // Reflection ile string tipindeki özellikleri kontrol et
      ValidateStrings(item);

      // Temel işleme işlemi
      return _decoratedProcessor.Process(item);
    }

    private void ValidateStrings(TModel item)
    {
      var properties = typeof(TModel).GetProperties()
                          .Where(p => p.PropertyType == typeof(string));

      foreach (var property in properties)
      {
        var value = (string)property.GetValue(item);

        // Null, Empty veya WhiteSpace kontrolü
        if (string.IsNullOrWhiteSpace(value))
        {
          throw new ArgumentException($"Property '{property.Name}' cannot be null, empty, or whitespace.");
        }
      }
    }
  }
  public class NumericFieldItemDecorator<TModel> : IItemProcessor<TModel>
  {
    private readonly IItemProcessor<TModel> _decoratedProcessor;

    public NumericFieldItemDecorator(IItemProcessor<TModel> decoratedProcessor)
    {
      _decoratedProcessor = decoratedProcessor;
    }

    public TModel Process(TModel item)
    {
      // Reflection ile sayısal alanları kontrol et
      ValidateNumericFields(item);

      // Temel işleme işlemi
      return _decoratedProcessor.Process(item);
    }

    private void ValidateNumericFields(TModel item)
    {
      // Sayısal özellikleri al
      var properties = typeof(TModel).GetProperties()
                          .Where(p => IsNumericType(p.PropertyType));

      foreach (var property in properties)
      {
        var value = property.GetValue(item);

        if (value == null || Convert.ToDecimal(value) <= 0)
        {
          throw new ArgumentException($"Property '{property.Name}' must be a valid positive numeric value.");
        }
      }

    }

    // Sayısal bir tip mi diye kontrol eder
    private bool IsNumericType(Type type)
    {
      return type == typeof(int) || type == typeof(double) || type == typeof(decimal)
             || type == typeof(float) || type == typeof(long);
    }

  }



  // DECORATOR PATTERN END

  #endregion

  #region ProxyPattern

  // Proxy PATTERN START
  public class CsvItemReaderProxy<TModel> : IItemReader<TModel>
  {
    private CsvItemReader<TModel> _realReader;  // Gerçek veri okuma işlemi yapan nesne

    public CsvItemReaderProxy(CsvItemReader<TModel> realReader)
    {
      _realReader = realReader;
    }

    // Dosya okumadan önce dosyanın var olup olmadığını kontrol ettikten sonra realReader nesnesini tetikliyor
    public List<TModel> Read()
    {
      List<TModel> items = new List<TModel>();

      if (FileExists(_realReader.FileName))
      {
        items = _realReader.Read();
        Console.WriteLine("Dosya Okuma İşlemi Başarılı Oldu. Csv Proxy");

        return items;
      }
      else
      {
        throw new FileNotFoundException();
      }

    }

    private bool FileExists(string fileName)
    {
      var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);

      return Path.Exists(filePath);
    }


  }


  // Proxy Pattern END

  #endregion

  #region AdapterPattern

  // ADAPTER PATTERN START

  // Target -> IItemReader<TModel>
  // Adapter -> ExternalCsvItemReaderAdapter
  // Adaptee -> ExternalCsvItemReader (Adapte edilecek sınıf)

  public class ExternalCsvItemReader<TModel>
  {
    private string _filename;

    public ExternalCsvItemReader(string filename)
    {
      _filename = filename;
    }

    public List<TModel> ReadItems()
    {
      return new List<TModel>();
    }
  }

  // 3prd Csv Item Reader Paketini Ana uygulamamızda kullanmak istiyorum. Performans gerekçesi ile
  public class ExternalCsvItemReaderAdapter<TModel> : IItemReader<TModel>
  {
    private string _filename;
    private ExternalCsvItemReader<TModel> reader;

    public ExternalCsvItemReaderAdapter(string fileName)
    {
      _filename = fileName;
      reader = new ExternalCsvItemReader<TModel>(_filename);
    }
    public List<TModel> Read()
    {
      return reader.ReadItems();
    }
  }

  // ADAPTER PATTERN END

  #endregion

  #region MediatorPattern

  // MEDIATOR START

  // Component A
  public interface IJobRequest
  {
    string JobName { get; set; }
    Dictionary<string, string> Parameters { get; set; }

  }

  // Mediator Interface
  public interface IJobMediator
  {
    void ExecuteJob(IJobRequest jobRequest);
  }

  // Component B -> A ile B haberleşecek
  public interface IJobExecutorHandler
  {
    void Execute(IJobRequest jobRequest);
  }

  // Concrete Mediator ise Mediator üzerinden Component A ile Component B arasındaki ilişkiyi sağlar.
  public class JobMediator : IJobMediator
  {
    private readonly Dictionary<string, IJobExecutorHandler> _jobHandlers;

    public JobMediator()
    {
      _jobHandlers = new Dictionary<string, IJobExecutorHandler>();
    }

    // Job türünü kaydeder
    public void RegisterJobHandler(string jobName, IJobExecutorHandler handler)
    {
      _jobHandlers[jobName] = handler;
    }

    public void ExecuteJob(IJobRequest jobRequest)
    {
      if (_jobHandlers.ContainsKey(jobRequest.JobName))
      {
        var handler = _jobHandlers[jobRequest.JobName];
        handler.Execute(jobRequest);
      }
      else
      {
        Console.WriteLine($"Job type {jobRequest.JobName} not found.");
      }
    }
  }

}


  // MEDIATOR END


#endregion