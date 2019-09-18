using NUnit.Framework;
using StatsdClient;
using Tests.Utils;
using Mono.Unix;
using System.Text;
using System.Net.Sockets;

namespace Tests
{
    [TestFixture]
    public class StatsdUnixDomainSocketTest
    {
        private TemporaryPath _temporaryPath;
        private Socket _server;
        private DogStatsdService _dogStatsdService;        

        [SetUp]
        public void Setup()
        {
            _temporaryPath = new TemporaryPath();
            var endPoint = new UnixEndPoint(_temporaryPath.Path);
            _server = new Socket(AddressFamily.Unix, SocketType.Dgram, ProtocolType.IP);            
            _server.Bind(endPoint);
            
            var dogstatsdConfig = new StatsdConfig
            {
                StatsdServerName = StatsdUnixDomainSocket.UnixDomainSocketPrefix + _temporaryPath.Path,
                MaxUnixDomainSocketPacketSize = 1000
            };
            _dogStatsdService = new DogStatsdService();
            _dogStatsdService.Configure(dogstatsdConfig);
        }

        void TearDown()
        {
            _server.Dispose();
            _dogStatsdService.Dispose();
            _temporaryPath.Dispose();        
        }

        [Test]
        public void SendSingleMetric()
        {          
            var metric = "gas_tank.level";
            var value = 0.75;
           _dogStatsdService.Gauge(metric, value);
           Assert.AreEqual($"{metric}:{value}|g", ReadFromServer());
        }      

        [Test]
        public void SendSplitMetrics()
        {   
            using (var statdUds = new StatsdUnixDomainSocket(StatsdUnixDomainSocket.UnixDomainSocketPrefix + _temporaryPath.Path, 25))
            {
                var statd = new Statsd(statdUds);
                var messageCount = 7;

                for (int i = 0; i < messageCount; ++i)
                    statd.Add("title" + i, "text");
                Assert.AreEqual(messageCount, statd.Commands.Count);

                statd.Send();

                var response = ReadFromServer();                
                for (int i = 0; i < messageCount; ++i)
                    Assert.True(response.Contains("title" + i));
            }            
        }        

        [Test]
        public void CheckNotBlockWhenServerNotReadMessage()
        {   
            var tags = new string[]{ new string('A', 100)};

            for (int i = 0; i < 1000; ++i)       
                _dogStatsdService.Gauge("metric" + i, 42, 1, tags);
            // If the code go here that means we do not block.
        } 
        string ReadFromServer()
        {    
            var builder = new StringBuilder();        
            var buffer = new byte[8096];            

            while (_server.Available > 0)            
            {            
                var count =  _server.Receive(buffer);
                var chars = System.Text.Encoding.UTF8.GetChars(buffer, 0, count);
                builder.Append(chars);
            }
            
            return builder.ToString();
        }                      
    }
}
