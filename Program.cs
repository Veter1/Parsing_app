using System;
using System.Collections;
using System.Threading;
using HtmlAgilityPack;
using System.Net;
using System.Data.SQLite;
using Telegram.Bot;
using System.Text;
using System.Text.RegularExpressions;

namespace Parse_app
{
    class Program
    {
        static public SQLiteConnection myConnection;

        // токен и клиент телеграм бота
        static string token { get; set; } = "2101161674:AAF1Cye3XSI94-AZQfE-vB69cQiZhwgXtR4";
        static TelegramBotClient bot_client;

        // список ссылок на новости из ленты
        static ArrayList list = new ArrayList();

        // для скачивания html-документов
        static HtmlWeb ws = new HtmlWeb();

        // для скачивания 'mail.ru'          ( - - НЕ ИСПОЛЬЗУЕТСЯ - - )
        static WebClient wc = new WebClient();

        // (клиент для скачивания картинок) НЕ ИСПОЛЬЗУТСЯ
        // static WebClient wClient = new WebClient();

        [Obsolete]
        static void Main(string[] args)
        {            
            while (true)
            {
                //для дебагигна
                Console.ForegroundColor = ConsoleColor.Gray;

                // для корректной работы
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;


                ukr_net_online();
                ukr_net();
                //mail_ru();

                // публикаця новостей в телеграм
                Telegram_bot_send();

                // 10-минутная пауза
                Console.WriteLine("----------------!Конец выполнения программы, перехожу в сон на 10 минут!----------------");
                Thread.Sleep(600000);
            }
        }

        // метод отправки новостей в телеграм канал
        [Obsolete]
        static void Telegram_bot_send()
        {
            //Console.WriteLine("\n --- Вызов телеграм бота --- \n");
            //Console.ForegroundColor = ConsoleColor.Yellow;
            //Console.WriteLine(news_href);
            //Console.ForegroundColor = ConsoleColor.Red;
            //Console.WriteLine(title);
            //Console.ForegroundColor = ConsoleColor.Gray;
            //Console.WriteLine(text);
            //Console.ForegroundColor = ConsoleColor.Blue;
            //Console.WriteLine(picture_href);
            //Console.ForegroundColor = ConsoleColor.Cyan;


            // подключаем базу, включаем бота
            myConnection = new SQLiteConnection("Data Source=database.sqlite3");
            myConnection.Open();
            bot_client = new TelegramBotClient(token);
            bot_client.StartReceiving();

            //Console.WriteLine("\n --- Поиск не опубликованых новостей --- \n");
            // ищем не опубликованные новости
            SQLiteCommand myCommand = new SQLiteCommand("SELECT news_href, title, text, picture_href FROM News_base WHERE status = 'не опубликованная'", myConnection);
            SQLiteDataReader result = myCommand.ExecuteReader();
            if (result.HasRows)
            {
                while (result.Read())
                {
                    //Console.Write("\n   Найдена неопубликованная новость:  \n");

                    // новости из 'ukr.net', без текста и картинки
                    if (result["text"].ToString() == "" && result["picture_href"].ToString() == "")
                    {
                        // отсылаем                        
                        bot_client.SendTextMessageAsync(-1001549131127, result["news_href"].ToString());
                        Thread.Sleep(3000);
                    }
                    else
                    {
                        // отсылаем
                        bot_client.SendPhotoAsync(-1001549131127, result["picture_href"].ToString(), result["title"].ToString());
                        Thread.Sleep(2000);
                        bot_client.SendTextMessageAsync(-1001549131127, result["text"].ToString() + "\n " + "Новина за посиланням: " + "\n" + result["news_href"].ToString());
                        Thread.Sleep(2000);
                    }
                }
            }

            // отмечаем что новости были опубликованы
            SQLiteCommand myCommand_1 = new SQLiteCommand("UPDATE News_base SET status = 'опубликованная' WHERE status = 'не опубликованная'", myConnection);
            myCommand_1.ExecuteNonQuery();

            // отключаем базу, выключаем бота
            myConnection.Close();
            bot_client.StopReceiving();

            //Console.WriteLine("\n --- Выключение телеграм бота --- \n");
        }

        // метод сохранения новостей в базу
        [Obsolete]
        static void Add_to_base(string news_href, string title, string text, string picture_href)
        {
            //Console.WriteLine("IT's Add_to_base");
            //Console.ForegroundColor = ConsoleColor.Yellow;
            //Console.WriteLine(news_href);
            //Console.ForegroundColor = ConsoleColor.Red;
            //Console.WriteLine(title);
            //Console.ForegroundColor = ConsoleColor.Gray;
            //Console.WriteLine(text);
            //Console.ForegroundColor = ConsoleColor.Blue;
            //Console.WriteLine(picture_href);
            //Console.ForegroundColor = ConsoleColor.Cyan;

            // корректируем текстовку
            text = text.Replace("&nbsp;", " ");
            text = text.Replace("&laquo;", "'");
            text = text.Replace("&raquo;", "'");
            text = text.Replace("&ndash;", "-");
            text = text.Replace("&raquo;", "'");

            // подключаем базу
            myConnection = new SQLiteConnection("Data Source=database.sqlite3");
            myConnection.Open();

            // сравниваем новую новость с теми что есть в базе (чтобы избежать дубликатов)
            SQLiteCommand myCommand = new SQLiteCommand("SELECT news_href FROM News_base", myConnection);            
            SQLiteDataReader result = myCommand.ExecuteReader();          
            bool resol = true;
            if (result.HasRows)
            {                
                while (result.Read())
                {
                    //Console.WriteLine("Data_href" + result["news_href"]);
                    //Console.WriteLine("L1 - " + news_href);
                    if (result["news_href"].ToString() == news_href)
                    {
                        resol = false;
                        //Console.WriteLine("-- Найдено совпадение, новая запись НЕ БУДЕТ добавлена в БД --");
                    }
                }                
            }

            //Console.WriteLine("resol - " + resol);
            // если новость уникальная - добавляем            
            if (resol)
            {
                // добавляем данные в базу
                SQLiteCommand myCommand_1 = new SQLiteCommand("INSERT INTO News_base('news_href', 'title', 'text', 'picture_href', 'status') VALUES(@news_href, @title, @text, @picture_href, @status)", myConnection);
                myCommand_1.Parameters.AddWithValue("@news_href", news_href);
                myCommand_1.Parameters.AddWithValue("@title", title);
                myCommand_1.Parameters.AddWithValue("@text", text);
                myCommand_1.Parameters.AddWithValue("@picture_href", picture_href);
                myCommand_1.Parameters.AddWithValue("@status", "не опубликованная");
                myCommand_1.ExecuteNonQuery();

                // (для скачки фото) ФОТО НЕ КАЧАЮ ПОТОМУ ЧТО БОТ ОТСЫЛАЕТ ЕГО В ТЕЛЕГУ ПО url АДРЕСУ                   
                // wClient.DownloadFile(picture_href, @".\Pictures\" + title + ".jpg");

                //Console.WriteLine("-- Новая запись успешно добавлена в БД --");

                //Console.WriteLine("telegram bot has wrote");
            }

            // отключаем базу
            myConnection.Close();    
        }

        // метод парсинга новостей с сайта "ukr-online.com"
        [Obsolete]
        static void ukr_net_online()
        {            
            //ws.OverrideEncoding = Encoding.UTF8;
            HtmlDocument doc = ws.Load("https://ukr-online.com");                       
                                   
            // получаем список ссылок из новостной ленты
            int count = 0;
            foreach (HtmlNode node in doc.DocumentNode.SelectNodes("//div[contains(@class, 'lastblockq')]//div[contains(@class, 'custom-4')]/a[@href]"))
            {
                count++;

                // вытягиваем и сохраняем список ссылок на новости из ленты
                //Console.WriteLine(" GET href = " + node.GetAttributeValue("href", null));
                list.Add(node.GetAttributeValue("href", null));

                if (count == 10) break;
            }

            // парсим все новости что на вытаскивали
            foreach (string silka in list)
            {
                // грузим новостную страницу для её парсинга
                doc = ws.Load(silka);

                string Text = "";

                //Console.WriteLine(); 
                //Console.WriteLine("парсим страницу = " + silka);
                //Console.WriteLine();

                //достаём заголовок
                foreach (HtmlNode title_link in doc.DocumentNode.SelectNodes("//div[contains(@class, 'post-title')]//span"))
                {
                    //достаём текстовку
                    foreach (HtmlNode text_link in doc.DocumentNode.SelectNodes("//div[contains(@id, 'hypercontext')]//p"))
                    {
                        Text += text_link.InnerText;
                    }

                    //достаём ссылку на картинку
                    foreach (HtmlNode picture_link in doc.DocumentNode.SelectNodes("//div[contains(@id, 'hypercontext')]//img"))
                    {
                        // передаём полученые данные на проверку и сохранение
                        Add_to_base(silka, title_link.InnerText, Text, picture_link.Attributes["src"].Value);
                        break;
                    }
                }                      
                              
            }
        }

        // метод парсинга новостей с сайта "ukr.net"
        [Obsolete]
        static void ukr_net()
        {            
            ws.OverrideEncoding = Encoding.UTF8;
            HtmlDocument doc = ws.Load("https://ukr.net");

            // получаем список ссылок из новостной ленты
            foreach (HtmlNode node in doc.DocumentNode.SelectNodes("//body//article[1]//a[@nc]"))
            {
                // передаём полученые данные на проверку и сохранение
                Add_to_base(node.GetAttributeValue("href", null), node.InnerText, "", "");
            }                          
            
        }

        // метод парсинга новостей с сайта "mail_ru"              ( - - НЕ ИСПОЛЬЗУЕТСЯ - - )
        static void mail_ru()
        {
            // заметка - прокси должен быть с поддержкой HTTPS
            // сайт со списком прокси: https://hidemy.name/ru/proxy-list/?country=US&type=s#list
            // здесь лучше - https://www.sslproxies.org/

            wc.Headers["User-Agent"] = "MOZILLA/5.0 (WINDOWS NT 6.1; WOW64) APPLEWEBKIT/537.1 (KHTML, LIKE GECKO) CHROME/21.0.1180.75 SAFARI/537.1";
            wc.Proxy = new WebProxy("188.0.241.198:8989");
            wc.Encoding = Encoding.UTF8;
            var page = wc.DownloadString("https://mail.ru");

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(page);

            //// для скачивания html-документов
            //HtmlWeb ws = new HtmlWeb();
            //ws.OverrideEncoding = Encoding.UTF8;
            //HtmlDocument doc = ws.Load("https://ukr.net");

            // получаем список ссылок из новостной ленты
            //int count = 0;
            foreach (HtmlNode node in doc.DocumentNode.SelectNodes("//body"))
            {
                //Console.WriteLine(node.GetAttributeValue("href", null));
                //Console.WriteLine(node.InnerHtml);

                //count++;

                bool result = Regex.IsMatch(node.InnerHtml, "\\target\\b");
                bool result_1 = Regex.IsMatch(node.InnerHtml, "\\data-testid\\b");
                if (result == true && result_1 == true)
                {
                    Console.WriteLine("\n ЕСТЬ! - " + node.InnerHtml + "\n");
                }
                Console.WriteLine("\n" + node.InnerHtml + "\n");

                // silka
                //Console.WriteLine(" GET href = " + node.GetAttributeValue("href", null));

                //list.Add(node.GetAttributeValue("href", null));
                //Console.WriteLine();

                //if (count == 1) break;
            }

            //// перебираем новостные страницы и парсим их
            //foreach (string silka in list)
            //{
            //    Thread.Sleep(500);

            //    // грузим новостную страницу для её парсинга
            //    doc = ws.Load(silka);

            //    //получаем дату
            //    foreach (HtmlNode link in doc.DocumentNode.SelectNodes("//div[contains(@class, 'post-info')]"))
            //    {
            //        Console.ForegroundColor = ConsoleColor.Red;
            //        Console.WriteLine(link.InnerText);
            //    }

            //    //получаем заголовок
            //    foreach (HtmlNode link in doc.DocumentNode.SelectNodes("//div[contains(@class, 'post-title')]//span"))
            //    {
            //        Console.ForegroundColor = ConsoleColor.Green;
            //        Console.WriteLine(link.InnerText);
            //    }

            //    //получаем текст
            //    foreach (HtmlNode link in doc.DocumentNode.SelectNodes("//div[contains(@id, 'hypercontext')]//p"))
            //    {
            //        Console.ForegroundColor = ConsoleColor.Gray;
            //        Console.WriteLine(link.InnerText);
            //    }

            //    ////получаем картинку
            //    //foreach (HtmlNode link in doc.DocumentNode.SelectNodes("//div[contains(@id, 'hypercontext')]//img"))
            //    //{
            //    //    // получили и вывели ссылку
            //    //    Console.ForegroundColor = ConsoleColor.Blue;
            //    //    Console.WriteLine("link for picture = " + link.Attributes["src"].Value);

            //    //    // качаем и сохраняем картинку                    
            //    //    wClient.DownloadFile(link.Attributes["src"].Value, @"D:\Avast\testimg.jpg");                   
            //    //}
            //}
        }

    }
}
