/////////////////////////////////////////////////////////////////////
// Repository.cs - holds test code for TestHarness                 //
//                                                                 //
// Saisumanth, CSE681 - Software Modeling and Analysis, Fall 2016  //
/////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * -------------------
 * Stores all the Testcode and log files
 * Queries for Logs and Libraries.
 * 
 * Required Files:
 * - Client.cs, ITest.cs, Logger.cs
 * Public Interface :
 *  EnqueueMessagesToRepo(Message):  Used by Reposervice to Enqueue messages
 *  UploadFile(filename, uri)       : Used to upload the file to the remote uri
 *  download(filename, uri)         : Though not used in this application, client
 *                                    can use this utility to download log files 
 *                                    from the Repository.
 *  Repository()                        : Constructor
 *  CreateSendChannel(uri)          : Generic method to remotely establish a 
 *                                    channel with the specified Uri.
 *  Close()                         : Closes the ServiceHost which stops further
 *                                    new connection establishment with Client.
 *  CreateRepoRecvChannel()         : Creates a new Http Binding and adds it to the
 *                                    service endpoint , exposes its services.
 *  queryLogs(queryText)            : Takes the string as input and searches the whole repository 
 *                                    for the file
 * 
 * Maintenance History:
 * --------------------
 * ver 1.0 : 18 Nov 2016
 * - first release
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.ServiceModel;
using System.Threading;

namespace TestHarness
{

    public class Repository : IRepository
    {
        private string repoStoragePath = "..\\..\\..\\Repository\\RepositoryStorage\\";
        public static SWTools.BlockingQueue<Message> inQ_ { get; set; }
        private static ServiceHost RepoService = null;
        private Thread RepoThrd = null;
        private string lastError = "";
   

        public static void EnqueueMessagesToRepo(Message m)
        {
            inQ_.enQ(m);
        }

        // Create proxy to another Peer's Communicator
        public IService createSendChannel(string address)
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
                //    Console.Write("\n Created proxy object to communicate with Test Harness");
                return factory.CreateChannel();
            }
            else
                return null;

        }

        public void CreateRepoRecvChannel(string address)
        {
            // Can't configure SecurityMode other than none with streaming.
            // This is the default for BasicHttpBinding.
            BasicHttpSecurityMode securityMode = BasicHttpSecurityMode.None;
            BasicHttpBinding binding = new BasicHttpBinding(securityMode);
            Uri baseAddress = new Uri(address);
            binding.TransferMode = TransferMode.Streamed;
            binding.MaxReceivedMessageSize = 50000000;
            RepoService = new ServiceHost(typeof(RepoService), baseAddress);
            RepoService.AddServiceEndpoint(typeof(IService), binding, baseAddress);
            RepoService.Open();
        }
        public void Close()
        {
            RepoService.Close();
        }
        void ThreadProc()
        {
            while (true)
            {
                Message msg = inQ_.deQ();
                if (msg.from.Contains("Client"))
                {
                    Console.WriteLine(); Console.WriteLine();
                    Console.WriteLine("Repository receieved a new message from Client which contains the Query Request"); Console.WriteLine();
                    msg.show();
                    Message newm = new Message("");
                    newm.from = "http://localhost:8080/RepoIService";
                    newm.to = msg.from;
                    Console.WriteLine("Searching in Repository for requested string...."); Console.WriteLine();
                    Console.WriteLine("The Results have been retrieved"); Console.WriteLine();
                    Console.WriteLine("Constructing the Message body with retrieved results"); Console.WriteLine();
                    List<string> result = queryLogs(msg.body);
                    foreach (string str in result)
                    {
                        newm.body += str + "\n";
                    }
                    IService Clientchannel = createSendChannel(msg.from);
                    if (Clientchannel != null)
                    {
                        Console.WriteLine(); Console.WriteLine("Message body is:");
                        Console.WriteLine(newm.body); Console.WriteLine();
                        Console.WriteLine("Sending query log to the client"); Clientchannel.PostMessage(newm);
                        Console.WriteLine("The Message has been sent to the Client");
                    }
                    else
                    {
                        Console.WriteLine("Client channel is not created hence cannot send query log");
                    }
                }
                else if (msg.from.Contains("Test"))
                {
                    Console.WriteLine(); Console.WriteLine(); Console.WriteLine("Repository receieved a new message from Test Harness");
                    Console.WriteLine();
                    msg.show();
                    if (getFiles(msg.from, msg.body) != true)
                    {
                        Console.WriteLine("Failed to process the code request from the TestHarness");
                    }
                }
            }
        }

        public Repository()
        {
            //   Console.Write("\n Creating instance of Repository");
            if (inQ_ == null)
                inQ_ = new SWTools.BlockingQueue<Message>();
            //   CreateRepoRecvChannel(RepoUrl);
            //   Console.Write("\n Created new Repository Service to accept http connections");
            RepoThrd = new Thread(ThreadProc);
            RepoThrd.IsBackground = true;
            RepoThrd.Start();
        }
        //----< search for text in log files >---------------------------
        /*
         * This function should return a message.  I'll do that when I
         * get a chance.
         */
        public List<string> queryLogs(string queryText)
        {
            List<string> queryResults = new List<string>();
            string path = System.IO.Path.GetFullPath(repoStoragePath);
            string[] files = System.IO.Directory.GetFiles(repoStoragePath, "*.txt");
            foreach (string file in files)
            {
                string contents = File.ReadAllText(file);
                if (contents.Contains(queryText))
                {
                    string name = System.IO.Path.GetFileName(file);
                    queryResults.Add(name);
                }
            }
            queryResults.Sort();
            queryResults.Reverse();
            return queryResults;
        }
        public void uploadFile(string filename, string Uri)
        {
            string fqname = Path.Combine(repoStoragePath, filename);
            IService Channel = null;
            /* Send to TestHarness if flag is 1 */

            Channel = createSendChannel(Uri);
            if (Channel == null)
            {
                Console.WriteLine("Repository Failed to establish connection with {0}", Uri);
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
                Console.WriteLine();
                Console.WriteLine("Uploaded file " + filename + " to Test Harness");
                Console.WriteLine();
            }
            catch
            {

                Console.WriteLine("*****Demonstrating Req3*******");
                Console.WriteLine("Can't find \"{0}\"", Path.GetFullPath(fqname));
                Console.WriteLine();
            }
        }

        public void download(string filename, string Uri)
        {
            IService Channel = null;
            int BlockSize = 1024;
            byte[] block = new byte[BlockSize];
            Channel = createSendChannel(Uri);
            if (Channel == null)
            {
                Console.WriteLine("Failed to download files from {0} ", Uri);
            }
            else
            {
                Channel = createSendChannel("http://localhost:8080/TestHarnessIService");
                if (Channel == null)
                    Console.WriteLine("Failed to download files from TestHarness");
            }
            int totalBytes = 0;
            try
            {
                Stream strm = Channel.downLoadFile(filename);
                string rfilename = Path.Combine(repoStoragePath, filename);
                if (!Directory.Exists(repoStoragePath))
                    Directory.CreateDirectory(repoStoragePath);
                using (var outputStream = new FileStream(rfilename, FileMode.Create))
                {
                    while (true)
                    {
                        int bytesRead = strm.Read(block, 0, BlockSize);
                        totalBytes += bytesRead;
                        if (bytesRead > 0)
                            outputStream.Write(block, 0, bytesRead);
                        else
                            break;
                    }
                }
                Console.Write("Received file \"{0}\" of {1} bytes.", filename, totalBytes);
            }
            catch (Exception ex)
            {
                Console.Write("\n  {0}", ex.Message);
            }
        }
        //----< send files with names on fileList >----------------------
        
        public bool getFiles(string uri, string fileList)
        {
            string[] files = fileList.Split(new char[] { ',' });
            //IService Channel = createSendChannel(uri);
            string repoStoragePath = "..\\..\\RepositoryStorage\\";
            foreach (string file in files)
            {
                //Console.WriteLine("File requested is : {0}", file);
                string fqSrcFile = repoStoragePath + file;
                // string fqDstFile = "";
                try
                {
                    uploadFile(file, uri);
                    //     fqDstFile = path + "\\" + file;
                    //     File.Copy(fqSrcFile, fqDstFile);
                }
                catch
                {
                    Console.Write("Could not Upload \"" + file);
                    return false;
                }
            }
            return true;
        }
       
        //#if (TEST_REPOSITORY)
        static void Main(string[] args)
        {
            Console.Title = "Repository";
            Console.WriteLine("==========================");
            Console.WriteLine("Starting of Repository");
            Console.WriteLine("==========================");
            Console.WriteLine();
            Console.WriteLine("Repository will receieve files from client which are to be Tested");
            Console.WriteLine();
            /*
             * ToDo: add code to test 
             * - Test code in Repository class that sends files to TestHarness.
             * - Modify TestHarness code that now copies files from RepositoryStorage folder
             *   to call Repository.getFiles.
             * - Add code to respond to client queries on files and logs.
             * - Add RepositoryTest class that implements ITest so Repo
             *   functionality can be tested in TestHarness.
             */
            try
            {
                Repository myrepo = new Repository();
                myrepo.CreateRepoRecvChannel("http://localhost:8080/RepoIService");
            }
            catch (Exception ex)
            {
                Console.Write("\n\n  {0}\n\n", ex.Message);
            }
            Console.ReadLine();
        }

        public void sendLog(string log)
        {
            throw new NotImplementedException();
        }

        //#endif
    }
}
