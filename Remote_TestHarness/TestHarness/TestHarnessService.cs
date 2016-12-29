/////////////////////////////////////////////////////////////////////
// ClientService.cs - Service class used by remote channels to     //
//                    create proxy objects and communicate with    // 
//                    Repository and client                        //
//  ver 1.0                                                        //
//  Language:      Visual C#  2015                                 //
//  Platform:      Mac, Windows 7                                  //
//  Application:   TestHarness - Project4                          //
//                 CSE681 - Software Modeling and Analysis,        //
//                 Fall 2016                                       //
//  Author:        saisumanth, Syracuse University                 //
//                 (315) 828-6589, sgopiset@syr.edu                //
//                                                                 //
//  Source:        Jim Fawcett                                     //
/////////////////////////////////////////////////////////////////////
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace TestHarness
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerSession)]
    class TestHarnessService : IService
    {
        public void PostMessage(Message msg)
        {
            TestHarness.EnqueueMessagesToTestHarness(msg);
        }
        public void upLoadFile(FileTransferMessage msg)
        {
            string dir = TestHarness.returnCurTempDir();
            int totalBytes = 0;
            int BlockSize = 1024;
            byte[] block = new byte[BlockSize];
            string filename = msg.filename;
            string rfilename = Path.Combine(dir, filename);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            using (var outputStream = new FileStream(rfilename, FileMode.Create))
            {
                while (true)
                {
                    int bytesRead = msg.transferStream.Read(block, 0, BlockSize);
                    totalBytes += bytesRead;
                    if (bytesRead > 0)
                        outputStream.Write(block, 0, bytesRead);
                    else
                        break;
                }
            }
            Console.Write("\n  Received file \"{0}\" of {1} bytes .",
                          filename, totalBytes);
            return;
        }
        public Stream downLoadFile(string filename)
        {
            return null;
        }
    }
}
