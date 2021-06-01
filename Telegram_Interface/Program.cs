using Bible_Bot.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram_Interface.Models;

namespace Telegram_Interface
{
    class Program
    {
        public static readonly string token = "1772505786:AAEt0wFHtaxQMtrHAsnxYVzGEYhrgnJWDeQ";
        private static TelegramBotClient client;

        [Obsolete]
        static void Main(string[] args)
        {
            client = new TelegramBotClient(token);
            client.StartReceiving();
            client.OnMessage += OnMessageHandler;
            client.OnCallbackQuery += BotOnCallbackQueryReceived;
            Console.ReadLine();
            client.StopReceiving();
        }
        private static async void BotOnCallbackQueryReceived(object sender, CallbackQueryEventArgs e)
        {
            string apiAddres = "https://biblebotapi.azurewebsites.net";
            var cl = new HttpClient();
            cl.BaseAddress = new Uri(apiAddres);

            var callbackQuery = e.CallbackQuery;

            if (Regex.IsMatch(callbackQuery.Data, @"bible(\w*)"))
            {
                string raw = $"{{\"userId\": \"{callbackQuery.Message.Chat.Id}\",\"bibleId\": \"{callbackQuery.Data.Substring(5)}\"}}";
                var data = new StringContent(raw, Encoding.UTF8, "application/json");

                var post = await cl.PostAsync($"BibleData/addUsersBibleDb", data);
                post.EnsureSuccessStatusCode();

                await client.SendTextMessageAsync(
                    chatId: callbackQuery.Message.Chat.Id,
                    text: $"The Bible with the id <{callbackQuery.Data.Substring(5)}> has been successfully set as default.\n Now you can process more.",
                    replyMarkup: MainMenu()
                );
            }
            else if (Regex.IsMatch(callbackQuery.Data, @"langu(\w*)"))
            {
                string[] forms = callbackQuery.Data.Substring(5).Split(',');
                await GetBibles(forms[0], forms[1], callbackQuery.Message.Chat.Id, cl);
            }
            else if (Regex.IsMatch(callbackQuery.Data, @"booke(\w*)"))
            {
                await GetSingleBook(callbackQuery.Data.Substring(5), callbackQuery.Message.Chat.Id, cl);
            }
            else if (Regex.IsMatch(callbackQuery.Data, @"verse(\w*)"))
            {
                await GetVerse(callbackQuery.Data.Substring(5), callbackQuery.Message.Chat.Id, cl);
            }
            else if (Regex.IsMatch(callbackQuery.Data, @"chapt(\w*)"))
            {
                await GetChapter(callbackQuery.Data.Substring(5), callbackQuery.Message.Chat.Id, cl);
            }
        }

        [Obsolete]
        private static async void OnMessageHandler(object sender, MessageEventArgs e)
        {
            string apiAddres = "https://biblebotapi.azurewebsites.net";
            var cl = new HttpClient();
            cl.BaseAddress = new Uri(apiAddres);

            List<Models.Language> langs = new List<Models.Language>() { new Models.Language { Id = "bel", Name = "Belarusian" }, new Models.Language { Id = "eng", Name = "English" }, new Models.Language { Id = "deu", Name = "German" }, new Models.Language { Id = "pol", Name = "Polish" } };

            var msg = e.Message;

            if (msg.Text != null || msg.Type != MessageType.Text)
            {
                switch (msg.Text)
                {
                    case "Set Bible":
                        await client.SendTextMessageAsync(
                                     chatId: msg.Chat.Id,
                                    "Choose one of the languages offered.",
                                    replyMarkup: new InlineKeyboardMarkup(GetLangKeyboard(langs))
                                    );
                        break;
                    case "Read some verses":

                        await GetMultipleBooks(msg.Chat.Id, cl);
                        break;

                    case "Search in Bible":
                        string bibleId = await GetUsersBible(msg.Chat.Id, cl);

                        if (bibleId == null)
                        {
                            await client.SendTextMessageAsync(
                            chatId: msg.Chat.Id,
                            $"At first you are to choose and set a Bible.",
                            replyMarkup: StartButton()
                            );
                        }
                        else
                        {
                            await client.SendTextMessageAsync(
                            chatId: msg.Chat.Id,
                            $"Enter the query.");

                            client.OnMessage -= OnMessageHandler;
                            client.OnMessage += GetString;
                            client.OnMessage += OnMessageHandler;
                        }

                        break;
                    case "Download Bible":
                        await GetFile(msg.Chat.Id, cl);
                        break;
                }
            }
            async void GetString(object sender, MessageEventArgs e)
            {
                var mess = e.Message;

                string bibleId = await GetUsersBible(msg.Chat.Id, cl);

                if (mess.Text != null || mess.Type != MessageType.Text)
                {
                    try
                    {
                        var versesQuery = await cl.GetAsync($"BibleData/getVersesQuery?BibleId={bibleId}&Query={mess.Text}&Offset=0");

                        var versesRaw = versesQuery.Content.ReadAsStringAsync().Result;
                        var versesProcessed = JsonConvert.DeserializeObject<VerseQuery>(versesRaw);

                        if (versesQuery.IsSuccessStatusCode && versesProcessed != null)
                        {
                            var keyboardMarkup = new InlineKeyboardMarkup(GetVersesQueryKeyboard(versesProcessed.Data));

                            await client.SendTextMessageAsync(
                            chatId: mess.Chat.Id,
                            $"Here are the references. Choose one to get text.",
                                  replyMarkup: keyboardMarkup
                            );
                        }
                        else
                        {
                            await client.SendTextMessageAsync(e.Message.From.Id, "Nothing was found.", replyMarkup: MainMenu());
                        }
                    }
                    catch
                    {
                        await client.SendTextMessageAsync(e.Message.From.Id, "Seems like smth is wrong.");
                    }
                    client.OnMessage -= GetString;

                }
            }
        }

        private static IReplyMarkup StartButton()
        {
            return new ReplyKeyboardMarkup
            {
                Keyboard = new List<List<KeyboardButton>>
                {
                    new List<KeyboardButton>{new KeyboardButton { Text = "Set Bible" } }
                }
            };
        }
        private static IReplyMarkup MainMenu()
        {
            return new ReplyKeyboardMarkup
            {
                Keyboard = new List<List<KeyboardButton>>
                {
                    new List<KeyboardButton>{new KeyboardButton { Text = "Read some verses" }, new KeyboardButton { Text = "Search in Bible" } },
                    new List<KeyboardButton>{new KeyboardButton { Text = "Download Bible" }, new KeyboardButton { Text = "Set Bible" } }
                }
            };
        }
        private static List<List<InlineKeyboardButton>> GetBooksKeyboard(List<BookDb> books)
        {
            List<List<InlineKeyboardButton>> arr = new List<List<InlineKeyboardButton>>();

            for (int i = 0; i < books.Count; i++)
            {
                var keyboardInline = new InlineKeyboardButton
                {
                    Text = books[i].Name,
                    CallbackData = "booke" + books[i].BookId,
                };
                List<InlineKeyboardButton> row = new List<InlineKeyboardButton>();
                row.Add(keyboardInline);

                arr.Add(row);
            }

            return arr;
        }
        private static List<List<InlineKeyboardButton>> GetChaptersKeyboard(List<string> chapters)
        {
            List<List<InlineKeyboardButton>> arr = new List<List<InlineKeyboardButton>>();

            for (int i = 0; i < chapters.Count; i++)
            {
                var keyboardInline = new InlineKeyboardButton
                {
                    Text = chapters[i],
                    CallbackData = "chapt" + chapters[i]
                };
                List<InlineKeyboardButton> row = new List<InlineKeyboardButton>();
                row.Add(keyboardInline);

                arr.Add(row);
            }
            return arr;
        }
        private static List<List<InlineKeyboardButton>> GetBibleKeyboard(List<BibleDb> bbls)
        {
            List<List<InlineKeyboardButton>> arr = new List<List<InlineKeyboardButton>>();

            for (int i = 0; i < bbls.Count; i++)
            {
                var keyboardInline = new InlineKeyboardButton
                {
                    Text = bbls[i].Name,
                    CallbackData = "bible" + bbls[i].BibleId,
                };
                List<InlineKeyboardButton> row = new List<InlineKeyboardButton>();
                row.Add(keyboardInline);

                arr.Add(row);
            }

            return arr;
        }
        private static List<List<InlineKeyboardButton>> GetLangKeyboard(List<Models.Language> langs)
        {
            List<List<InlineKeyboardButton>> arr = new List<List<InlineKeyboardButton>>();

            for (int i = 0; i < langs.Count; i++)
            {
                var keyboardInline = new InlineKeyboardButton
                {
                    Text = langs[i].Name,
                    CallbackData = $"langu{langs[i].Id},{langs[i].Name}"
                };
                List<InlineKeyboardButton> row = new List<InlineKeyboardButton>();
                row.Add(keyboardInline);

                arr.Add(row);
            }

            return arr;
        }
        private static List<List<InlineKeyboardButton>> GetVersesKeyboard(ChapterResp chpt)
        {
            List<List<InlineKeyboardButton>> arr = new List<List<InlineKeyboardButton>>();

            for (int i = 1; i < chpt.VerseCount; i++)
            {
                var keyboardInline = new InlineKeyboardButton
                {
                    Text = $"{chpt.Reference}:{i}",
                    CallbackData = $"verse{chpt.ChapterId}.{i}"
                };

                List<InlineKeyboardButton> row = new List<InlineKeyboardButton>();
                row.Add(keyboardInline);

                arr.Add(row);
            }

            return arr;
        }
        private static List<List<InlineKeyboardButton>> GetVersesQueryKeyboard(QueryVersesData data)
        {
            List<List<InlineKeyboardButton>> arr = new List<List<InlineKeyboardButton>>();

            for (int i = 1; i < data.Verses.Count; i++)
            {
                var keyboardInline = new InlineKeyboardButton
                {
                    Text = $"{data.Verses[i].Reference}",
                    CallbackData = $"verse{data.Verses[i].Id}"
                };

                List<InlineKeyboardButton> row = new List<InlineKeyboardButton>();
                row.Add(keyboardInline);

                arr.Add(row);
            }

            return arr;
        }
        public static async Task GetBibles(string lang, string langFull, long chatId, HttpClient cl)
        {
            var isOnDb = await cl.GetAsync($"BibleData/getBiblesDb?Lang={lang}");

            if (!isOnDb.IsSuccessStatusCode)
            {
                var isOnPublic = await cl.GetAsync($"BibleData/getBiblesPublic?Language={lang}");
                isOnPublic.EnsureSuccessStatusCode();

                var biblesPublic = isOnPublic.Content.ReadAsStringAsync().Result;
                var bibles = JsonConvert.DeserializeObject<BiblesPublic>(biblesPublic);

                if (isOnPublic.IsSuccessStatusCode)
                {
                    List<BibleDb> biblesObj = new List<BibleDb>();
                    for (int i = 0; i < bibles.Data.Count; i++)
                    {
                        BibleDb tempBbl = new BibleDb
                        {
                            BibleId = bibles.Data[i].Id,
                            Name = bibles.Data[i].Name,
                            NameLocal = bibles.Data[i].NameLocal,
                            Abbreviation = bibles.Data[i].Abbreviation,
                            AbbreviationLocal = bibles.Data[i].AbbreviationLocal,
                            Language = bibles.Data[i].Language.Id
                        };
                        biblesObj.Add(tempBbl);
                    }

                    await client.SendTextMessageAsync(
                    chatId: chatId,
                    $"Here are the Bibles for your language ({langFull.ToLower()})",
                          replyMarkup: new InlineKeyboardMarkup(GetBibleKeyboard(biblesObj))
                    );
                }
                else
                {
                    await client.SendTextMessageAsync(
                     chatId: chatId,
                    "fail;(",
                    replyMarkup: StartButton()
                    );
                }

                var jsonBibles = JsonConvert.SerializeObject(bibles);
                var data = new StringContent(jsonBibles, Encoding.UTF8, "application/json");

                var post = await cl.PostAsync("BibleData/addBiblesDb", data);
                post.EnsureSuccessStatusCode();

            }
            else
            {
                var biblesDb = isOnDb.Content.ReadAsStringAsync().Result;
                var bibles = JsonConvert.DeserializeObject<List<BibleDb>>(biblesDb);

                var keyboardMarkup = new InlineKeyboardMarkup(GetBibleKeyboard(bibles));

                await client.SendTextMessageAsync(
                chatId: chatId,
                $"Here are the Bibles for your language ({langFull.ToLower()})",
                      replyMarkup: keyboardMarkup
                );
            }
        }
        public static async Task<string> GetUsersBible(long chatId, HttpClient cl)
        {
            var isOnDb = await cl.GetAsync($"BibleData/getUsersBibleDb?userId={chatId}");

            var biblesDb = isOnDb.Content.ReadAsStringAsync().Result;
            if (biblesDb == "no")
                return null;

            var bible = JsonConvert.DeserializeObject<Models.User>(biblesDb);

            return bible.BibleId;
        }
        public static async Task GetMultipleBooks(long chatId, HttpClient cl)
        {
            string bibleId = await GetUsersBible(chatId, cl);

            if (bibleId == null)
            {

                await client.SendTextMessageAsync(
                chatId: chatId,
                $"At first you are to choose and set a Bible.",
                replyMarkup: StartButton()
                );
            }
            else
            {
                var isOnDb = await cl.GetAsync($"BibleData/getMultipleBooksDb?bibleId={bibleId}");

                if (!isOnDb.IsSuccessStatusCode)
                {
                    var isOnPublic = await cl.GetAsync($"BibleData/getMultipleBooksPublic?bibleId={bibleId}");
                    isOnPublic.EnsureSuccessStatusCode();

                    var booksPublic = isOnPublic.Content.ReadAsStringAsync().Result;
                    var books = JsonConvert.DeserializeObject<List<BookDb>>(booksPublic);

                    if (isOnPublic.IsSuccessStatusCode)
                    {
                        await client.SendTextMessageAsync(
                        chatId: chatId,
                        $"At first you should choose a book.\nHere are the books of your Bible.",
                              replyMarkup: new InlineKeyboardMarkup(GetBooksKeyboard(books))
                        );
                    }
                    else
                    {
                        await client.SendTextMessageAsync(
                         chatId: chatId,
                        "fail;(",
                        replyMarkup: StartButton()
                        );
                    }

                    var jsonBooks = JsonConvert.SerializeObject(books);
                    var data = new StringContent(jsonBooks, Encoding.UTF8, "application/json");

                    var post = await cl.PostAsync("BibleData/addMultipleBooksDb", data);
                    post.EnsureSuccessStatusCode();
                }
                else
                {
                    var biblesDb = isOnDb.Content.ReadAsStringAsync().Result;
                    var books = JsonConvert.DeserializeObject<List<BookDb>>(biblesDb);

                    var keyboardMarkup = new InlineKeyboardMarkup(GetBooksKeyboard(books));

                    await client.SendTextMessageAsync(
                    chatId: chatId,
                    $"At first you should choose a book.\nHere are the books of your Bible.",
                          replyMarkup: keyboardMarkup
                    );
                }
            }
        }
        public static async Task GetSingleBook(string bookId, long chatId, HttpClient cl)
        {
            string bibleId = await GetUsersBible(chatId, cl);

            var isOnDb = await cl.GetAsync($"BibleData/getSingleBookDb?book={bibleId}{bookId}");

            if (!isOnDb.IsSuccessStatusCode)
            {
                var isOnPublic = await cl.GetAsync($"BibleData/getSingleBookPublic?bibleId={bibleId}&bookId={bookId}");
                isOnPublic.EnsureSuccessStatusCode();

                var booksPublic = isOnPublic.Content.ReadAsStringAsync().Result;
                var books = JsonConvert.DeserializeObject<BookChaptsInclude>(booksPublic);

                if (isOnPublic.IsSuccessStatusCode)
                {
                    await client.SendTextMessageAsync(
                    chatId: chatId,
                    $"Now choose a chapter, please.",
                          replyMarkup: new InlineKeyboardMarkup(GetChaptersKeyboard(books.Chapters))
                    );
                }
                else
                {
                    await client.SendTextMessageAsync(
                     chatId: chatId,
                    "fail;(",
                    replyMarkup: StartButton()
                    );
                }

                var jsonBooks = JsonConvert.SerializeObject(books);
                var data = new StringContent(jsonBooks, Encoding.UTF8, "application/json");

                var post = await cl.PostAsync("BibleData/addSingleBookToDb", data);
                post.EnsureSuccessStatusCode();
            }
            else
            {
                var biblesDb = isOnDb.Content.ReadAsStringAsync().Result;
                var books = JsonConvert.DeserializeObject<BookChaptsInclude>(biblesDb);

                var keyboardMarkup = new InlineKeyboardMarkup(GetChaptersKeyboard(books.Chapters));

                await client.SendTextMessageAsync(
                chatId: chatId,
                $"Now choose a chapter, please.",
                      replyMarkup: keyboardMarkup
                );
            }
        }
        public static async Task GetChapter(string chapterId, long chatId, HttpClient cl)
        {
            string bibleId = await GetUsersBible(chatId, cl);

            var isOnDb = await cl.GetAsync($"BibleData/getChapterDb?bibleId={bibleId}&chapterId={chapterId}");

            if (!isOnDb.IsSuccessStatusCode)
            {
                var isOnPublic = await cl.GetAsync($"BibleData/getChapterPublic?bibleId={bibleId}&chapterId={chapterId}");
                isOnPublic.EnsureSuccessStatusCode();

                var booksPublic = isOnPublic.Content.ReadAsStringAsync().Result;
                var books = JsonConvert.DeserializeObject<ChapterResp>(booksPublic);

                if (isOnPublic.IsSuccessStatusCode)
                {
                    await client.SendTextMessageAsync(
                    chatId: chatId,
                    "Finally, choose your verses.",
                          replyMarkup: new InlineKeyboardMarkup(GetVersesKeyboard(books))
                    );
                }
                else
                {
                    await client.SendTextMessageAsync(
                     chatId: chatId,
                    "fail;(",
                    replyMarkup: StartButton()
                    );
                }

                var jsonBooks = JsonConvert.SerializeObject(books);
                var data = new StringContent(jsonBooks, Encoding.UTF8, "application/json");

                var post = await cl.PostAsync("BibleData/addChapterDb", data);
                post.EnsureSuccessStatusCode();
            }
            else
            {
                var biblesDb = isOnDb.Content.ReadAsStringAsync().Result;
                var books = JsonConvert.DeserializeObject<ChapterResp>(biblesDb);

                var keyboardMarkup = new InlineKeyboardMarkup(GetVersesKeyboard(books));

                await client.SendTextMessageAsync(
                chatId: chatId,
                "Finally, choose your verses.",
                      replyMarkup: keyboardMarkup
                );
            }
        }
        public static async Task GetVerse(string verseId, long chatId, HttpClient cl)
        {
            string bibleId = await GetUsersBible(chatId, cl);

            var isOnDb = await cl.GetAsync($"BibleData/getVerseDb?BibleId={bibleId}&VerseId={verseId}");

            if (!isOnDb.IsSuccessStatusCode)
            {
                var isOnPublic = await cl.GetAsync($"BibleData/getVersePublic?BibleId={bibleId}&VerseId={verseId}");
                isOnPublic.EnsureSuccessStatusCode();

                var booksPublic = isOnPublic.Content.ReadAsStringAsync().Result;
                var books = JsonConvert.DeserializeObject<Verse>(booksPublic);

                if (isOnPublic.IsSuccessStatusCode)
                {
                    await client.SendTextMessageAsync(
                    chatId: chatId,
                    $"{books.Reference}\n{books.Content}",
                          replyMarkup: MainMenu()
                    );
                }
                else
                {
                    await client.SendTextMessageAsync(
                     chatId: chatId,
                    "fail;(",
                    replyMarkup: StartButton()
                    );
                }

                var jsonBooks = JsonConvert.SerializeObject(books);
                var data = new StringContent(jsonBooks, Encoding.UTF8, "application/json");

                var post = await cl.PostAsync("BibleData/addVerseDb", data);
                post.EnsureSuccessStatusCode();
            }
            else
            {
                var biblesDb = isOnDb.Content.ReadAsStringAsync().Result;
                var books = JsonConvert.DeserializeObject<ChapterResp>(biblesDb);

                await client.SendTextMessageAsync(
                chatId: chatId,
                $"{books.Reference}\n{books.Content}",
                      replyMarkup: MainMenu()
                );
            }
        }
        [Obsolete]
        public static async Task GetFile(long chatId, HttpClient cl)
        {
            string bibleId = await GetUsersBible(chatId, cl);

            if (bibleId == null)
            {
                await client.SendTextMessageAsync(
                chatId: chatId,
                $"At first you are to choose and set a Bible.",
                replyMarkup: StartButton()
                );
            }
            else
            {
                await client.SendTextMessageAsync(
                chatId: chatId,
                "This may take some time as we have to fully rely on public API.\nWe will inform you about our steps.",
                replyMarkup: StartButton()
                );

                var BibleObj = await cl.GetAsync($"BibleData/getMultipleBooksPublic?bibleId={bibleId}");
                BibleObj.EnsureSuccessStatusCode();

                var booksContent = BibleObj.Content.ReadAsStringAsync().Result;
                var booksList = JsonConvert.DeserializeObject<List<BookDb>>(booksContent);

                await client.SendTextMessageAsync(
                chatId: chatId,
                "We have found the list of books of your Bible.",
                replyMarkup: StartButton()
                );

                if (BibleObj.IsSuccessStatusCode)
                {
                    string BibleFile = "";

                    for (int i = 0; i < booksList.Count && i <= 5; i++)
                    {
                        var bookSingle = await cl.GetAsync($"BibleData/getSingleBookPublic?bibleId={bibleId}&bookId={booksList[i].BookId}");
                        bookSingle.EnsureSuccessStatusCode();

                        var booksPublic = bookSingle.Content.ReadAsStringAsync().Result;
                        var book = JsonConvert.DeserializeObject<BookChaptsInclude>(booksPublic);

                        for (int c = 0; c < book.Chapters.Count && c <= 3; c++)
                        {
                            var chapterContent = await cl.GetAsync($"BibleData/getChapterPublic?bibleId={bibleId}&chapterId={book.Chapters[c]}");
                            chapterContent.EnsureSuccessStatusCode();

                            var chapterPublic = chapterContent.Content.ReadAsStringAsync().Result;
                            var chapter = JsonConvert.DeserializeObject<ChapterResp>(chapterPublic);

                            BibleFile += chapter.Content + "\n";
                        }
                    }

                    await client.SendTextMessageAsync(
                    chatId: chatId,
                    "Finally, we have created a <string> object of your book.",
                    replyMarkup: StartButton()
                    );
                    string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Yop.txt"); ;
                    try
                    {
                        using (StreamWriter sw = new StreamWriter(path, false, Encoding.Default))
                        {
                            sw.WriteLine(BibleFile);
                        }

                        System.IO.File.WriteAllText(path, BibleFile);

                        await client.SendTextMessageAsync(
                        chatId: chatId,
                        "Hurray, we have saved a text file with a Bible.",
                        replyMarkup: StartButton()
                        );

                        using (var stream = System.IO.File.OpenRead(path))
                        {
                            InputOnlineFile iof = new InputOnlineFile(stream);
                            iof.FileName = "Bible";
                            var send = await client.SendDocumentAsync(chatId, iof, "You are welcome!");
                        }
                    }
                    catch (Exception e)
                    {

                        await client.SendTextMessageAsync(
                        chatId: chatId,
                        "Smth went wrong(",
                        replyMarkup: StartButton()
                        );

                    }
                }
                else
                {
                    await client.SendTextMessageAsync(
                     chatId: chatId,
                    "fail;(",
                    replyMarkup: StartButton()
                    );
                }
            }
        }
    }
}
