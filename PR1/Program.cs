using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Net;
using ImageMagick;
using System.Linq;
using OpenQA.Selenium.Support.UI;
using OpenQA.Selenium.Remote;

namespace PR1
{
    class Program
    {
        static void NormalizeNumbers(string filePath, string newFilePath, string badNumbersFilePath, string goodNumbersFilePath)
        {
            File.WriteAllText(newFilePath, string.Empty);
            using (StreamReader sr = new StreamReader(filePath, Encoding.Default))
            {
                Regex regex = new Regex(@"^[a-zA-Z0-9]+$");
                string line;
                string successNumber;
                while ((line = sr.ReadLine()) != null)
                {
                    StreamReader srg = new StreamReader(goodNumbersFilePath, Encoding.Default);
                    StreamReader srb = new StreamReader(badNumbersFilePath, Encoding.Default);
                    if (regex.IsMatch(line))
                    {
                        bool check = false;
                        while ((successNumber = srg.ReadLine()) != null)
                            if (successNumber == line)
                            {
                                check = true;
                                goto checkPos;
                            }
                        while ((successNumber = srb.ReadLine()) != null)
                            if (successNumber == line)
                            {
                                check = true;
                                goto checkPos;
                            }
                        checkPos: if (!check)
                            using (StreamWriter sw = new StreamWriter(newFilePath, true, Encoding.Default))
                                sw.WriteLine(line);

                    }
                    else
                    {
                        StringBuilder result = new StringBuilder();
                        using (StreamWriter sw = new StreamWriter(newFilePath, true, Encoding.Default))
                        {
                            int i = 0;
                            foreach (var newstr in line)
                                if (newstr != '.' && newstr != '_' && newstr != '-')
                                    result.Insert(i++, newstr);

                            bool check = false;
                            while ((successNumber = srg.ReadLine()) != null)
                                if (successNumber == result.ToString())
                                {
                                    check = true;
                                    goto checkPos;
                                }
                            while ((successNumber = srb.ReadLine()) != null)
                                if (successNumber == result.ToString())
                                {
                                    check = true;
                                    goto checkPos;
                                }
                            checkPos: if (!check)
                                sw.WriteLine(result);
                        }
                    }
                }

            }
        }
        static void ParseData(string filePath, string downloadedFilesFolder, string badNumbersFilePath, string goodNumbersFilePath)
        {

            ChromeOptions options = new ChromeOptions();
            options.AddArgument("--headless");
            //options.AddArgument("--user-agent=Mozilla/5.0 (iPad; CPU OS 6_0 like Mac OS X) AppleWebKit/536.26 (KHTML, like Gecko) Version/6.0 Mobile/10A5355d Safari/8536.25");
            var driver = new ChromeDriver(options);
            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(3);
            driver.Url = @"https://aftermarket.zf.com/ru/ru/sachs/catalogs/#/";
            driver.Manage().Window.Maximize();
            driver.FindElement(By.XPath(@".//button[@id='cookie-banner-action']")).Click();
            using (StreamReader sr = new StreamReader(filePath, Encoding.Default))
            {
                string line;
                IJavaScriptExecutor js = driver as IJavaScriptExecutor;
                while ((line = sr.ReadLine()) != null)
                {
                    driver.FindElement(By.XPath(@".//div[@class='search-input-wrapper']/input")).Clear();
                    driver.FindElement(By.XPath(@".//div[@class='search-input-wrapper']/input")).SendKeys(line + Keys.Enter);
                    js.ExecuteScript("window.scrollBy(0,250);");
                    var elements = driver.FindElements(By.LinkText($"{line}"));
                    if (elements.Count > 0)
                    {

                        elements[0].SendKeys(Keys.Enter);
                        var Images = driver.FindElements(By.XPath(@".//div[@id='detailCarousel']/img"));
                        Console.WriteLine($"Number of images:{Images.Count},{line}");
                        if (Images.Count > 0)
                        {
                            bool check = false;
                            for (int i = 0, numbpict = 0; i < Images.Count; i++)
                            {
                                try
                                {
                                    var ImageUrl = Images[i].GetAttribute("src");
                                    var ImageName = Images.Count == 1 ? $"{line}" : $"{line}_{numbpict++}";
                                    Console.WriteLine($"Downloding file:{ImageName}");
                                    WebClient downloader = new WebClient();
                                    downloader.DownloadFile(ImageUrl, downloadedFilesFolder + ImageName + ".jpg");
                                }
                                catch (WebException e)
                                {
                                    Console.WriteLine($"Image is empty:{line},{e.Status}");
                                    check = true;
                                    Console.WriteLine($"Number {line} wasnt downloaded");
                                    using (StreamWriter sw = new StreamWriter(badNumbersFilePath, true, Encoding.Default))
                                        sw.WriteLine(line);
                                    break;
                                }
                            }
                            if (!check)
                                using (StreamWriter sw = new StreamWriter(goodNumbersFilePath, true, Encoding.Default))
                                    sw.WriteLine(line);
                        }
                        else
                        {
                            Console.WriteLine($"Number {line} wasnt downloaded");
                            using (StreamWriter sw = new StreamWriter(badNumbersFilePath, true, Encoding.Default))
                                sw.WriteLine(line);
                        }
                        Console.WriteLine();
                    }
                    else
                    {
                        Console.WriteLine($"Number {line} not founded");
                        using (StreamWriter sw = new StreamWriter(badNumbersFilePath, true, Encoding.Default))
                            sw.WriteLine(line);
                        Console.WriteLine();
                    }
                    driver.Navigate().GoToUrl("https://aftermarket.zf.com/ru/ru/sachs/catalogs/#/");
                }

            }
            Console.WriteLine("Images were downloaded!");
            driver.Quit();
        }
        static void GetImage(string LoadImagePath, string SaveImagePath, int quality, int maxSize)
        {
            using (MagickImage image = new MagickImage(LoadImagePath))
            {
                if (image.Width > image.Height)
                    image.Resize(maxSize, image.Height);
                else
                    image.Resize(image.Width, maxSize);
                image.Format = MagickFormat.Jpg;
                image.Quality = quality;
                image.Write(SaveImagePath);
            }

        }
        static void UpdateImages(string downloadedFilesFolder, string readyFilesFolder, int maxSizeImage, int qualityImage)
        {
            string lip = downloadedFilesFolder;
            string sip = readyFilesFolder;
            var Images = Directory
                 .GetFiles(lip, "*", SearchOption.AllDirectories)
                 .ToList();
            Console.WriteLine($"Images:{Images.Count}");

            for (int i = 0; i < Images.Count; i++)
            {
                Console.WriteLine($"Images №:{i} , Image Name:{Path.GetFileName(Images[i])}");
                GetImage(Images[i], $"{sip}{Path.GetFileName(Images[i])}", qualityImage, maxSizeImage);
                Console.WriteLine($"Images №:{i} was updated");
                Console.WriteLine();
            }
            Console.WriteLine("Images were updated");
        }
        static void Algorithm(
            string filePath,
            string newFilePath,
            string badNumbersFilePath,
            string goodNumbersFilePath,
            string downloadedFilesFolder,
            string readyFilesFolder,
            int maxSizeImage,
            int qualityImage
            )
        {
            NormalizeNumbers(filePath, newFilePath, badNumbersFilePath, goodNumbersFilePath);
            ParseData(newFilePath, downloadedFilesFolder, badNumbersFilePath, goodNumbersFilePath);
            UpdateImages(downloadedFilesFolder, readyFilesFolder, maxSizeImage, qualityImage);
        }
        static void Error()
        {
            Console.WriteLine("Enter arguments " +
                    "args[0] = file Path, " +
                    "args[1] = file with normalize numbers" +
                    "args[2] = folder for images, " +
                    "args[3] = folder for ready images, " +
                    "args[4] = max side size image, " +
                    "args[5] = quality of the image, " +
                    "args[6] = file for bad numbers, " +
                    "args[7] = file for good numbers");
        }
        static void Main(string[] args)
        {

            if (args.Length == 0)
            {
                Console.WriteLine("Arguments werent entered\n");
                Error();
                return;
            }
            string filePath = "", newFilePath = "", badNumbersFilePath = "", goodNumbersFilePath = "", downloadedFolder = "", readyFolder = "";
            int maxSize = 0, quality = 0;
            bool check = false;
            if (args.Length > 0)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    switch (args[i])
                    {
                        case "sachs.txt":
                            {
                                filePath = args[i];
                                break;
                            }
                        case "uptadednumbers.txt":
                            {
                                newFilePath = args[i];
                                break;
                            }
                        case @"DownloadedImages\":
                            {
                                downloadedFolder = args[i];
                                break;
                            }
                        case @"ReadyImages\":
                            {
                                readyFolder = args[i];
                                break;
                            }
                        case "200":
                            {
                                maxSize = int.Parse(args[i]);
                                break;
                            }
                        case "80":
                            {
                                quality = int.Parse(args[i]);
                                break;
                            }
                        case "badnumbers.txt":
                            {
                                badNumbersFilePath = args[i];
                                break;
                            }
                        case "goodnumbers.txt":
                            {
                                goodNumbersFilePath = args[i];
                                break;
                            }
                        default:
                            {
                                Console.WriteLine($"{args[i]} argument not founded\n");
                                check = true;
                                break;
                            }
                    }
                }
                if (check || args.Length!=8)
                    Error();
            }

            if (!check && args.Length==8)
                Algorithm(filePath, newFilePath, badNumbersFilePath,goodNumbersFilePath,downloadedFolder,readyFolder, maxSize, quality);

        }
    }

}
