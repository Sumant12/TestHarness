//  ver 1.0                                                        //
//  Language:      Visual C#  2015                                 //
//  Platform:      Mac, Windows 7                                  //
//  Application:   TestHarness - Project4                          //
//                 CSE681 - Software Modeling and Analysis,        //
//                 Fall 2016                                       //
//  Author:        Saisumanth, Syracuse University                 //
//                 (315) 248-8289, sgoipiset@syr.edu                //
//                                                                 //
//  Source:        Jim Fawcett                                     //
/////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * -------------------
 * The module basically creates a ServiceHost to accept the remote connections
 * and uses a child thread to process those messages enqueued into a Blocking
 * Queue. The main thread is repsonsible for sending the Test Requests to the
 * TestHarness and Query logs to the Repository remotely by using channels to
 * connect to them and posting the messages to their Blocking Queues. The module
 * also filestream services provided by ClientService.cs to send the entire code
 * to be tested to the Repository before beginning of its operation.
 * 
 * Required Files:
 * - Client.cs, ITest.cs, BlockingQueue.cs, Messages.cs, IService.cs
 * 
 * Public Interface :
 *  EnqueueMessages(Message): Used by ClientService to Enqueue messages
 *  UploadFile(filename, uri)       : Used to upload the file to the remote uri
 *  download(filename, uri)         : Though not used in this application, client
 *                                    can use this utility to download log files 
 *                                    from the Repository.
 *  Client()                        : Constructor
 *  CreateSendChannel(uri)          : Generic method to remotely establish a 
 *                                    channel with the specified Uri.
 *  Close()                         : Closes the ServiceHost which stops further
 *                                    new connection establishment with Client.
 *  CreateClientRecvChannel()       : Creates a new Http Binding and adds it to the
 *                                    service endpoint , exposes its services.
 *  sendTestRequest(Message)        : Post the message to the test harness remotely.
 *  makeQuery(queryText, Uri)       : Query the Repository for specified string .
 *  buildTestMessage(from, to)      : Build a test message that contains the xml formatted
 *                                    list of test drivers and codes to be tested.
 *  
 * 
 * Maintenance History:
 * --------------------
 * ver 1.0 : 18 November 2016
 * - first release
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TestHarness
{
    public class Client : IClient
    {
        public static SWTools.BlockingQueue<Message> inQ_ { get; set; }
        private static ServiceHost clientService = null;
        Thread ClientReadThrd = null;
        string lastError = "";
        string ToSendPath = "..\\..\\FilesToSend";
        private static HiResTimer hrt = new HiResTimer();
        private static HiResTimer hrt2 = new HiResTimer();


        //----< adds the Message to the Blocking Queue >-------------------------------
        public static void EnqueueMessages(Message m)
        {
            inQ_.enQ(m);
        }

        //----< Uploads file to the destination Uri >-------------------------------
        public void UploadFile(string filename, string Uri)
        {
            string fqname = Path.Combine(ToSendPath, filename);
            IService Channel = null;
            Channel = CreateSendChannel(Uri);
            if (Channel == null)
            {
                Console.WriteLine("Failed to establish connection with {0}", Uri);
                return;
            }

            try
            {
                using (var inputStream = new FileStream(fqname, FileMode.Open))
                {
                    FileTransferMessage msg = new FileTransferMessage();
                    msg.filename = filename;
                    msg.transferStream = inputStream;
                    Channel.upLoadFile(msg);
                }
                Console.WriteLine("Uploaded file " + filename + " to Repository");
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Can't find \"{0}\" Exception: {1}", fqname, ex.Message);
            }
        }
   

        //----< A thread which always executes if a message is posted in the Blocking Queue>-------------------------------
        void ThreadProc()
        {
            while (true)
            {
                Message msg = inQ_.deQ();
                hrt.Stop();


                if (msg.body == "quit")
                {
                    Close();
                    break;
                }

                Console.WriteLine();
                Console.WriteLine("Client received a new message");
                Console.WriteLine();
                msg.show();
                Console.WriteLine();
                ulong time = hrt.ElapsedMicroseconds;
                Console.WriteLine("Total Time of Execution is:");
                Console.WriteLine(time);
                {

                }
            }
        }

        //----< Constructor for the Client >-------------------------------
        public Client()
        {

            if (inQ_ == null)
                inQ_ = new SWTools.BlockingQueue<Message>();
            ClientReadThrd = new Thread(ThreadProc);
            ClientReadThrd.IsBackground = true;
            ClientReadThrd.Start();
        }

        //----<creates send Channel for the Client >-------------------------------
        public IService CreateSendChannel(string address)
        {
            int tryCount = 0;
            int MaxCount = 10;
            ChannelFactory<IService> factory = null;
            while (true)
            {
                try
                {
                    BasicHttpSecurityMode securityMode = BasicHttpSecurityMode.None;
                    EndpointAddress baseAddress = new EndpointAddress(address);
                    BasicHttpBinding binding = new BasicHttpBinding(securityMode);
                    binding.TransferMode = TransferMode.Streamed;
                    binding.MaxReceivedMessageSize = 500000000;
                    factory = new ChannelFactory<IService>(binding, address);
                    tryCount = 0;
                    break;
                }
                catch (Exception ex)
                {
                    if (++tryCount < MaxCount)
                    {
                        Thread.Sleep(100);
                        Console.WriteLine("Retrying {0} times to establish communication with testharness",
                                       tryCount);
                    }
                    else
                    {
                        lastError = ex.Message;
                        break;
                    }
                }
            }
            if (factory != null)
            {
                return factory.CreateChannel();
            }
            else
            {
                Console.Write("\n Failed to create proxy object to communicate with Test Harness");
                return null;
            }
        }

        //----<stops the client if a message is receieved a Quit >-------------------------------
        public void Close()
        {
            clientService.Close();
        }


        //----<creates receive Channel for the Client >-------------------------------
        public void CreateClientRecvChannel(string address)
        {
            // Can't configure SecurityMode other than none with streaming.
            // This is the default for BasicHttpBinding.
            BasicHttpSecurityMode securityMode = BasicHttpSecurityMode.None;
            BasicHttpBinding binding = new BasicHttpBinding(securityMode);
            Uri baseAddress = new Uri(address);
            binding.TransferMode = TransferMode.Streamed;
            binding.MaxReceivedMessageSize = 50000000;
            clientService = new ServiceHost(typeof(ClientService), baseAddress);
            clientService.AddServiceEndpoint(typeof(IService), binding, baseAddress);
            clientService.Open();
        }

        //----<Sends Test Request to the Test Harness >-------------------------------
        public void sendTestRequest(Message testRequest)
        {
            IService Thchannel = CreateSendChannel("http://localhost:8080/TestHarnessIService");
            if (Thchannel != null)
            {
                Thchannel.PostMessage(testRequest);
            }
            else
            {
                Console.WriteLine("Failed to post th request to the Test Harness");
            }
        }
        public void sendResults(Message results)
        {
            Console.WriteLine("\n  " + results.ToString());
            Console.WriteLine();
        }

        //----<Makes query to the repository >-------------------------------
        public void makeQuery(string queryText, string fromUri)
        {
            IService Repochannel = CreateSendChannel("http://localhost:8080/RepoIService");
            if (Repochannel != null)
            {
                Message m = new Message(queryText);
                m.from = fromUri;
                m.to = "http://localhost:8080/RepoIService";
                m.author = "SaisumanthGopisetty";
                Repochannel.PostMessage(m);
            }
            else
            {
                Console.WriteLine("Failed to post the query request to the Repository");
            }
        }

        //----<Builds message >-------------------------------
        Message buildTestMessage()
        {
            Message msg = new Message();
            msg.to = "TH";
            msg.from = "CL";
            msg.author = "Saisumanth Gopisetty";

            testElement te1 = new testElement("test1");
            te1.addDriver("testdriver.dll");
            te1.addCode("testedcode.dll");
            testElement te2 = new testElement("test2");
            te2.addDriver("td1.dll");
            te2.addCode("tc1.dll");
            testElement te3 = new testElement("test3");
            te3.addDriver("anothertestdriver.dll");
            te3.addCode("anothertestedcode.dll");
            testElement tlg = new testElement("loggerTest");
            tlg.addDriver("logger.dll");
            testRequest tr = new testRequest();
            tr.author = "Saisumanth Gopisetty";
            tr.tests.Add(te1);
            tr.tests.Add(te2);
            tr.tests.Add(te3);
            msg.body = tr.ToString();
            return msg;
        }

        static void Main(string[] args)
        {
            Console.Title = "Client1";
            Console.WriteLine("==========================");
            Console.WriteLine("Starting of Client1");
            Console.WriteLine("==========================");
            try
            {
                Client myClient1 = new Client();
               string ClientStream = "http://localhost:8080/ClientIStreamService";
                string RepoStream = "http://localhost:8080/RepoIService";
                string TestHarnessStream = "http://localhost:8080/TestHarnessIService";
                myClient1.CreateClientRecvChannel(ClientStream);
                Thread.Sleep(5000);
                /* Send all the code to be tested to the Repository */
                string filepath = Path.GetFullPath(myClient1.ToSendPath);
                Console.WriteLine();
                Console.WriteLine("*****Demonstarting Req6***********");
                Console.WriteLine("Files are uploaded to the Repository");
                Console.WriteLine();
                Console.WriteLine("The Path from which files are selected and uploaded into Repository: ");
                Console.WriteLine(filepath);
                Console.WriteLine();
                string[] files = Directory.GetFiles(filepath);
                foreach (string file in files)
                {
                    string filename = Path.GetFileName(file);
                    Console.WriteLine("File being uploaded is " + filename);

                    myClient1.UploadFile(filename, RepoStream);
                }
                Thread.Sleep(10000);
                Console.WriteLine("Buidling the Message which is sent to Test Harness");
                Console.WriteLine();
                Message msg = myClient1.buildTestMessage();
                Console.WriteLine("Message Built is:");
                Console.WriteLine();
                msg.show();
                Console.WriteLine();
                msg.from = ClientStream;
                msg.to = TestHarnessStream;
                Console.WriteLine("Sending the Message to Test Harness..");
                Console.WriteLine();
                Console.WriteLine("********Demonstrating req12**********");
                Console.WriteLine("Calcutaing the Execution time using Timer");
                Console.WriteLine();

                hrt.Start();
                myClient1.sendTestRequest(msg);
                /* Wait for the TestHarness to complete the execution before
                 sending the query to the reposiotry*/
                Thread.Sleep(30000);
                Console.WriteLine();
                Console.WriteLine("********Demonstrating req3**********");
                Console.WriteLine("if the files are not present in the Repo they are shown as file not loaded");
                Console.WriteLine();
              
                Console.WriteLine("********Demonstrating req9**********");
                Console.WriteLine("Client can query for a text");
      
                Console.WriteLine("Querying the Repository for the string test1");
                myClient1.makeQuery("test1", ClientStream);
            }
            catch (Exception ex)
            {
                Console.Write("\n\n  {0}\n\n", ex.Message);
            }
            Console.ReadLine();
        }
    }
}
