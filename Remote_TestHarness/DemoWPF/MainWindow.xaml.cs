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
 * A module which opens a wpf window and takes input form the User to perfotm Testing
 * The module also creates a ServiceHost to accept the remote connections
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
 *  QListner                        :listens for query text and passes the message to 
 *                                   Repository
 *  submitToTH                      :Submits the TestRequest to TestHarness
 * 
 * Maintenance History:
 * --------------------
 * ver 1.0 : 18 November 2016
 * - first release
 */
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.IO;
using System.Xml.Linq;
using TestHarness;
using System.Threading;
using System.ServiceModel;

namespace TestHarness
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class ClientWPF : Window, IClient
    {
        private string XMLMessage = "";
        private static int count = 1;
        private static string repochannel = "";
        public static SWTools.BlockingQueue<Message> inQ_ { get; set; }
        private ServiceHost clientService = null;
        private Thread ClientReadThrd = null;
        private string lastError = "";
        private string ToSendPath = "..\\..\\FilesToSend";
        private delegate void NewMessage(Message msg);
        private static HiResTimer hrt = new HiResTimer();
        private event NewMessage OnNewMessage;

        public ClientWPF()
        {

            Console.Title = "ClientGUI";
            InitializeComponent();
            OnNewMessage += new NewMessage(OnNewMessageHandler);
            if (inQ_ == null)
                inQ_ = new SWTools.BlockingQueue<Message>();

            ClientReadThrd = new Thread(ThreadProc);
            ClientReadThrd.IsBackground = true;
            ClientReadThrd.Start();



        }
        public void OnNewMessageHandler(Message msg)
        {


            if (msg.from.Contains("TestHarness"))
            {
                Console.WriteLine("Client receieved a new Message from TestHarness");
                msg.show();
                textBoxResult.Text = msg.body;
            }

            if (msg.from.Contains("Repo"))
            {
                Console.WriteLine("Client receieved a new Message from Repository");
                msg.show();
                if (msg.body == "" || msg.body == null)
                {
                    textBoxRepo.Text = "No Results obtained in Repository";
                }

                textBoxRepo.Text = msg.body;
            }

        }

        public void uploadFile(string filename, string Uri)
        {
            string fqname = System.IO.Path.Combine(ToSendPath, filename);
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





        public static void EnqueueMessagesToClient(Message m)
        {
            inQ_.enQ(m);
        }
        void ThreadProc()
        {
            while (true)
            {
                Message msg = inQ_.deQ();
                if (msg.body == "quit")
                {
                    Close();
                    break;
                }
                //}


                //Console.WriteLine();
                //Console.WriteLine("Client received a new message");
                //Console.WriteLine();
                //msg.show();
                //Console.WriteLine();


                this.Dispatcher.BeginInvoke(
        System.Windows.Threading.DispatcherPriority.Normal,
        OnNewMessage,
       msg);


            }
        }


        // Create proxy to another Peer's Communicator
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
                        Console.Write("Retrying {0} times to establish communication with testharness",
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
                //    Console.Write("\n Creating proxy object to communicate with Test Harness");
                return factory.CreateChannel();
            }
            else
            {
                Console.Write("\n Failed to create proxy object to communicate with Test Harness");
                return null;
            }
        }

        public void CreateClientRecvChannel(string address)
        {
            BasicHttpBinding binding = new BasicHttpBinding();
            Uri baseAddress = new Uri(address);
            binding.TransferMode = TransferMode.Streamed;
            binding.MaxReceivedMessageSize = 50000000;
            clientService = new ServiceHost(typeof(ClientService), baseAddress);
            clientService.AddServiceEndpoint(typeof(IService), binding, baseAddress);
            clientService.Open();
        }

        public void Close()
        {
            clientService.Close();
        }

        public void sendTestRequest(Message testRequest)
        {
            IService Thchannel = CreateSendChannel("http://localhost:8080/TestHarnessIService");
            if (Thchannel != null)
            {
                Thchannel.PostMessage(testRequest);
            }
            else
            {
                Console.WriteLine("Failed to post the request to the Test Harness");
            }
        }
        public void sendResults(Message results)
        {
            Console.Write("\n  Client received results message:");
            Console.Write("\n  " + results.ToString());
            Console.WriteLine();
        }
        public void makeQuery(string queryText, string fromUri)
        {
            IService Repochannel = CreateSendChannel("http://localhost:8080/RepoIService");
            if (Repochannel != null)
            {
                Message m = new Message(queryText);
                m.from = fromUri;
                m.to = "http://localhost:8080/RepoIService";
                m.author = "jashwanth";
                Repochannel.PostMessage(m);
            }
            else
            {
                Console.WriteLine("Failed to post the query request to the Repository");
            }
        }
        private void btnOpenFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
                txtEditor.Text = File.ReadAllText(openFileDialog.FileName);

        }


        private void SubmitToTH(object sender, RoutedEventArgs e)
        {

            XMLMessage = txtEditor.Text;
            BuildMessage(XMLMessage);
        }
        public void BuildMessage(string messagebody)
        {
            ClientWPF cl1 = new ClientWPF();
            string RepoStream = "http://localhost:8080/RepoIService";
            Message msg = new Message();
            msg.body = messagebody;
            Console.WriteLine("The Message body which is being sent to TestHarness: ");
            Console.WriteLine(msg.body);
            string frommessage = "http://localhost:8080/ClientIService" + count++;
            msg.author = "Saisumanth Gopisetty";
            msg.to = "TH";
            msg.from = frommessage;
            cl1.CreateClientRecvChannel(msg.from);

            /* Send all the code to be tested to the Repository */
            string filepath = System.IO.Path.GetFullPath(cl1.ToSendPath);
            Console.WriteLine("The Path from which files are selected and uploaded into Repository: ");
            Console.WriteLine(filepath);
            Console.WriteLine();
            string[] files = Directory.GetFiles(filepath);
            hrt.Start();
            foreach (string file in files)
            {
                string filename = System.IO.Path.GetFileName(file);
                Console.WriteLine("File being uploaded is " + filename);
                Console.WriteLine();
                cl1.uploadFile(filename, RepoStream);

            }
            Thread.Sleep(10000);
            Console.WriteLine("Sending the Message to Test Harness..");

            cl1.sendTestRequest(msg);
            hrt.Stop();
            ulong time = hrt.ElapsedMicroseconds;

            TimerTextBox.Text = time.ToString();
            Console.WriteLine("Total Elapsed time is" + time);
            Thread.Sleep(20000);

        }
        public void QListner(object sender, RoutedEventArgs e)
        {

            ClientWPF c2 = new ClientWPF();
            repochannel = "http://localhost:8080/ClientIService" + count++;
            c2.CreateClientRecvChannel(repochannel);
            string queryToBeQueried = RemotePortTextBox.Text;
            Console.WriteLine();
            Console.WriteLine("The query string given as input by Client is: ");
            Console.WriteLine(queryToBeQueried);
            Console.WriteLine();
            Console.WriteLine("Querying the Repository for the string");
            c2.makeQuery(queryToBeQueried, repochannel);
        }
        private void ResultBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void textBoxRepo_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}
