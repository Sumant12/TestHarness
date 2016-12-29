/////////////////////////////////////////////////////////////////////
// RepoService.cs - Service class used by remote channels to       //
//                  create proxy objects and communicate with      // 
//                  Repository                                     //
//  ver 1.0                                                        //
//  Language:      Visual C#  2015                                 //
//  Platform:      Mac, Windows 7                                  //
//  Application:   TestHarness - Project4                          //
//                 CSE681 - Software Modeling and Analysis,        //
//                 Fall 2016                                       //
//  Author:        JSaisumanth, Syracuse University                //
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
    class RepoService : IService
    {
        int BlockSize = 1024;
        byte[] block = null;
        static string repoStoragePath = "..\\..\\..\\Repository\\RepositoryStorage\\";
        public Stream downLoadFile(string filename)
        {
            string sfilename = Path.Combine(repoStoragePath, filename);
            FileStream outStream = null;
            if (File.Exists(sfilename))
            {
                outStream = new FileStream(sfilename, FileMode.Open);
            }
            else
            {
                Console.WriteLine("open failed for \"" + filename + "\"");
                return null;
            }
            Console.Write("\n  Sent \"{0}\" .", filename);
            return outStream;
        }

        public RepoService()
        {
            block = new byte[BlockSize];
        }
        public void PostMessage(Message msg)
        {
          // Console.WriteLine("Repository received a new message:");
           //   msg.show();
           Repository.EnqueueMessagesToRepo(msg);
        }

        public void upLoadFile(FileTransferMessage msg)
        {
            int totalBytes = 0;
            string filename = msg.filename;
            string rfilename = Path.Combine(repoStoragePath, filename);
            if (!Directory.Exists(repoStoragePath))
                Directory.CreateDirectory(repoStoragePath);
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
            Console.WriteLine("Received file \"{0}\" of {1} bytes ",
                          filename, totalBytes);
        }
    }
}
