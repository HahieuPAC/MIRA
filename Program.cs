using OpenQA.Selenium;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Support.UI;
using OpenQA.Selenium.Interactions;
using SeleniumExtras.WaitHelpers;
using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Diagnostics;
using System.Linq;

class Program
{
    static IWebDriver? klokDriver = null;
    static IWebDriver? chatGptDriver = null;
    static bool isRunning = true;
    static readonly string KLOK_URL = "https://klokapp.ai/app";
    static readonly string CHATGPT_URL = "https://chatgpt.com/c/67cf813f-dce4-800c-ac3e-787f0c39c0f3";
    static readonly string METAMASK_PASSWORD = "H@trunghj3up@c112358";
    
    // Thêm hằng số cho đường dẫn profile
    static readonly string BASE_EDGE_USER_DATA_DIR = GetEdgeUserDataDir();
    static readonly string CHATGPT_USER_DATA_DIR = Path.Combine(
        Path.GetDirectoryName(BASE_EDGE_USER_DATA_DIR) ?? "",
        "Edge",
        "User Data",
        "ChatGPT"
    ).Replace(
        Path.Combine("Edge", "Edge"),
        "Edge"
    );

    static void Main()
    {
        Console.CancelKeyPress += (sender, e) => {
            e.Cancel = true;
            isRunning = false;
            CleanupAndExit();
            Environment.Exit(0);
        };

        try
        {
            Console.WriteLine("🚀 Đang khởi động chương trình...");
            
            KillAllEdgeProcesses();
            Thread.Sleep(2000);

            // Cấu hình riêng cho Kite
            var kiteOptions = ConfigureEdgeOptions(true);
            // Cấu hình riêng cho ChatGPT với thư mục riêng
            var chatGptOptions = ConfigureEdgeOptions(false);
            
            Console.WriteLine("Nhấn Ctrl+C để dừng chương trình...");

            // Mở Kite và xử lý Metamask
            Console.WriteLine("Dang mo Klokapp...");
            klokDriver = new EdgeDriver(kiteOptions);
            klokDriver.Navigate().GoToUrl(KLOK_URL);
            Console.WriteLine("Da mo Klokapp thanh cong");

            bool isKlokLoggedIn = false;

            // Kiểm tra xem đã đăng nhập chưa
            try
            {
                Console.WriteLine("Kiem tra trang thai dang nhap Klokapp...");
                string chatInputXPath = "/html/body/div[1]/div[2]/div[2]/div[2]/div[1]";
                
                // Đợi trang load
                Thread.Sleep(3000);
                
                try
                {
                    var chatInput = klokDriver.FindElement(By.XPath(chatInputXPath));
                    if (chatInput != null)
                    {
                        Console.WriteLine("Da tim thay phan tu chat input -> Da dang nhap!");
                        isKlokLoggedIn = true;
                    }
                }
                catch (NoSuchElementException)
                {
                    Console.WriteLine("Chua dang nhap -> Tien hanh dang nhap...");
                    // Xử lý đăng nhập
                    HandleKlokLogin(klokDriver);
                    
                    // Kiểm tra lại sau khi đăng nhập
                    Thread.Sleep(5000);
                    try {
                        var chatInput = klokDriver.FindElement(By.XPath(chatInputXPath));
                        if (chatInput != null) {
                            Console.WriteLine("Dang nhap thanh cong!");
                            isKlokLoggedIn = true;
                        }
                    } catch (NoSuchElementException) {
                        Console.WriteLine("Dang nhap that bai!");
                        isKlokLoggedIn = false;
                    }
                }

                // Chỉ mở ChatGPT nếu đã đăng nhập Klokapp thành công
                if (isKlokLoggedIn)
                {
                    Console.WriteLine("🤖 Đang mở ChatGPT...");
                        Thread.Sleep(2000);
                        
                        try
                        {
                            chatGptDriver = new EdgeDriver(chatGptOptions);
                            chatGptDriver.Navigate().GoToUrl(CHATGPT_URL);
                        Console.WriteLine("✅ Đã mở ChatGPT thành công!");

                        Console.WriteLine("⌛ Đợi ChatGPT khởi động...");
                        Thread.Sleep(5000);

                        var wait = new WebDriverWait(chatGptDriver, TimeSpan.FromSeconds(10));

                        // Đợi và lấy câu trả lời từ ChatGPT
                        Console.WriteLine("⌛ Đợi ChatGPT trả lời...");
                        try 
                        {
                            // Đợi cho đến khi không còn thấy "Generating..."
                            wait.Until(d => {
                                try {
                                    var generating = d.FindElement(By.XPath("//div[contains(text(), 'Generating')]"));
                                    Console.WriteLine("⌛ ChatGPT đang trả lời...");
                                    return false;
                                }
                                catch (NoSuchElementException) {
                                    return true;
                                }
                            });

                            // Đợi thêm 1 giây để đảm bảo nội dung đã load hoàn tất
                            Thread.Sleep(1000);

                            // Lấy câu trả lời mới nhất
                            var responses = chatGptDriver.FindElements(By.CssSelector("div.markdown"));
                            if (responses.Count > 0)
                            {
                                var lastResponse = responses.Last();
                                string chatGptResponse = lastResponse.Text;

                                if (!string.IsNullOrEmpty(chatGptResponse))
                                {
                                    Console.WriteLine("\n🤖 ChatGPT trả lời:");
                                    Console.WriteLine("------------------------------------------");
                                    Console.WriteLine(chatGptResponse);
                                    Console.WriteLine("------------------------------------------");
                                    Console.WriteLine($"📏 Độ dài câu trả lời: {chatGptResponse.Length} ký tự");

                                    // Sau khi có câu trả lời, tìm textarea của Klok
                                    Console.WriteLine("\n[INFO] Tìm textarea của Klok...");
                                    var textareas = klokDriver.FindElements(By.TagName("textarea"));
                                    var klokInput = textareas.FirstOrDefault(t => 
                                        t.Displayed && 
                                        t.Enabled);

                                    if (klokInput != null)
                                    {
                                        Console.WriteLine("[SUCCESS] Đã tìm thấy textarea của Klok!");
                                        klokInput.Clear();
                                        klokInput.SendKeys(chatGptResponse);
                                        klokInput.SendKeys(Keys.Enter);
                                        Console.WriteLine("[SUCCESS] Đã gửi tin nhắn!");
                                        
                                        // Đợi phản hồi từ Klok
                                        Console.WriteLine("[INFO] Đợi phản hồi từ Klok...");
                                        Thread.Sleep(7000);
                                    }
                                    else
                                    {
                                        Console.WriteLine("[ERROR] Không tìm thấy textarea của Klok!");
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"❌ Lỗi khi xử lý câu trả lời: {ex.Message}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Lỗi khi mở ChatGPT: {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine("Khong the mo ChatGPT vi chua dang nhap Klokapp!");
                    CleanupAndExit();
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Loi khi kiem tra trang thai dang nhap: {ex.Message}");
                CleanupAndExit();
                return;
            }

            // Thêm phần tìm và click button với log chi tiết
            try 
            {
                Console.WriteLine("Doi trang web load...");
                Thread.Sleep(3000);

                string buttonXPath = "/html/body/div[1]/div/div[4]/button[2]";
                Console.WriteLine($"Dang tim nut voi XPath: {buttonXPath}");
                
                // Tìm button
                IWebElement? button = null;
                try 
                {
                    button = klokDriver.FindElement(By.XPath(buttonXPath));
                    
                    if (button != null)
                    {
                        Console.WriteLine("Da tim thay nut!");
                        Console.WriteLine($"Trang thai nut - Hien thi: {button.Displayed}, Kich hoat: {button.Enabled}");
                        Console.WriteLine($"Text tren nut: {button.Text}");
                        Console.WriteLine($"Class cua nut: {button.GetAttribute("class")}");

                        if (button.Displayed && button.Enabled)
                        {
                            Console.WriteLine("Dang thu click vao nut...");
                            button.Click();
                            Console.WriteLine("Da click vao nut!");
                        }
                        else 
                        {
                            Console.WriteLine("Nut khong the click duoc!");
                            Console.WriteLine($"- Hien thi: {button.Displayed}");
                            Console.WriteLine($"- Kich hoat: {button.Enabled}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Khong tim thay nut!");
                    }
                }
                catch (NoSuchElementException)
                {
                    Console.WriteLine("Khong tim thay nut, thu lai sau 5 giay...");
                    Thread.Sleep(5000);
                    
                    try
                    {
                        Console.WriteLine("Dang tim nut lan thu 2...");
                        button = klokDriver.FindElement(By.XPath(buttonXPath));
                        
                        if (button != null)
                        {
                            Console.WriteLine("Da tim thay nut trong lan thu 2!");
                            Console.WriteLine($"Trang thai nut - Hien thi: {button.Displayed}, Kich hoat: {button.Enabled}");
                            button.Click();
                            Console.WriteLine("Da click vao nut!");
                        }
                    }
                    catch (Exception retryEx)
                    {
                        Console.WriteLine($"Loi khi tim nut lan thu 2: {retryEx.Message}");
                        Console.WriteLine("Hien thi HTML hien tai de kiem tra:");
                        Console.WriteLine(klokDriver.PageSource.Substring(0, 500) + "...");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Loi khong xac dinh: {ex.Message}");
                    Console.WriteLine($"Loai loi: {ex.GetType().Name}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Loi khi xu ly nut: {ex.Message}");
            }

            // Sau khi click Connect Wallet thành công
            try
            {
                Console.WriteLine("Doi popup hien thi...");
                Thread.Sleep(2000);

                string metamaskButtonXPath = "//*[@id=\"__CONNECTKIT__\"]/div/div/div/div[2]/div[2]/div[3]/div/div/div/div[1]/div[1]/div/button[1]/span";
                Console.WriteLine("Dang tim nut Metamask trong popup...");

                IWebElement? metamaskButton = null;
                try
                {
                    metamaskButton = klokDriver.FindElement(By.XPath(metamaskButtonXPath));
                    
                    if (metamaskButton != null)
                    {
                        Console.WriteLine("Da tim thay nut Metamask!");
                        Console.WriteLine($"Text tren nut: {metamaskButton.Text}");
                        Console.WriteLine($"Trang thai nut - Hien thi: {metamaskButton.Displayed}, Kich hoat: {metamaskButton.Enabled}");

                        if (metamaskButton.Displayed && metamaskButton.Enabled)
                        {
                            Console.WriteLine("Dang click vao nut Metamask...");
                            metamaskButton.Click();
                            Console.WriteLine("Da click vao nut Metamask!");
                            
                            // Đợi Metamask popup xuất hiện
                            Thread.Sleep(2000);
                            
                            // Xử lý Metamask popup (nếu cần)
                            HandleMetamask(klokDriver);
                        }
                        else
                        {
                            Console.WriteLine("Nut Metamask khong the click!");
                            Console.WriteLine($"- Hien thi: {metamaskButton.Displayed}");
                            Console.WriteLine($"- Kich hoat: {metamaskButton.Enabled}");
                        }
                    }
                }
                catch (NoSuchElementException)
                {
                    Console.WriteLine("Khong tim thay nut Metamask, thu lai...");
                    Thread.Sleep(3000);
                    
                    try
                    {
                        Console.WriteLine("Tim nut Metamask lan 2...");
                        metamaskButton = klokDriver.FindElement(By.XPath(metamaskButtonXPath));
                        if (metamaskButton != null && metamaskButton.Displayed)
                        {
                            metamaskButton.Click();
                            Console.WriteLine("Da click nut Metamask trong lan thu 2!");
                        }
                    }
                    catch (Exception retryEx)
                    {
                        Console.WriteLine($"Khong the tim nut Metamask lan 2: {retryEx.Message}");
                        Console.WriteLine("HTML hien tai:");
                        Console.WriteLine(klokDriver.PageSource.Substring(0, 500) + "...");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Loi khi xu ly popup Metamask: {ex.Message}");
                Console.WriteLine($"Loai loi: {ex.GetType().Name}");
            }

            // Sau khi xử lý Metamask thành công
            Console.WriteLine("Cho 5 giay sau khi dang nhap Metamask...");
            Thread.Sleep(5000);

            try 
            {
                string signInButtonXPath = "/html/body/div[1]/div/div[4]/button";
                Console.WriteLine("Dang tim nut Sign In...");
                
                IWebElement? signInButton = null;
                try 
                {
                    signInButton = klokDriver.FindElement(By.XPath(signInButtonXPath));
                    
                    if (signInButton != null)
                    {
                        Console.WriteLine("Da tim thay nut Sign In!");
                        Console.WriteLine($"Text tren nut: {signInButton.Text}");
                        Console.WriteLine($"Trang thai nut - Hien thi: {signInButton.Displayed}, Kich hoat: {signInButton.Enabled}");

                        if (signInButton.Displayed && signInButton.Enabled)
                        {
                            Console.WriteLine("Dang click vao nut Sign In...");
                            signInButton.Click();
                            Console.WriteLine("Da click vao nut Sign In!");
                            
                            // Sau khi click Sign In, đợi và xử lý cửa sổ Metamask mới
                            Console.WriteLine("Doi cua so Metamask moi xuat hien...");
                            Thread.Sleep(3000);

                            // Tìm và xử lý cửa sổ Metamask mới
                            string mainWindow = klokDriver.CurrentWindowHandle;
                            bool foundMetamaskWindow = false;

                            foreach (string handle in klokDriver.WindowHandles)
                            {
                                if (handle != mainWindow)
                                {
                                    klokDriver.SwitchTo().Window(handle);
                                    if (klokDriver.Title == "MetaMask")
                                    {
                                        Console.WriteLine("Da tim thay cua so Metamask moi!");
                                        foundMetamaskWindow = true;

                                        // Tab 7 lần để đến nút Confirm
                                        Console.WriteLine("\nTab 7 lan den nut Confirm...");
                                        var actions = new Actions(klokDriver);
                                        for (int i = 0; i < 7; i++)
                                        {
                                            actions.SendKeys(Keys.Tab).Perform();
                                            Thread.Sleep(500);
                                            Console.WriteLine($"Tab lan {i + 1}");
                                        }

                                        // Click nút Confirm
                                        Console.WriteLine("Click nut Confirm...");
                                        actions.SendKeys(Keys.Enter).Perform();
                                        Console.WriteLine("Da click nut Confirm!");
                                        Thread.Sleep(2000);

                                        // Chuyển về cửa sổ chính
                                        klokDriver.SwitchTo().Window(mainWindow);
                                        break;
                                    }
                                }
                            }

                            if (!foundMetamaskWindow)
                            {
                                Console.WriteLine("Khong tim thay cua so Metamask moi!");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Loi khi tim hoac click nut Sign In: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Loi khi xu ly nut Sign In: {ex.Message}");
                Console.WriteLine($"Loai loi: {ex.GetType().Name}");
            }

            // Mở ChatGPT trong cửa sổ mới
            Console.WriteLine("🤖 Đang mở ChatGPT...");
            Thread.Sleep(2000);
            
            try
            {
                chatGptDriver = new EdgeDriver(chatGptOptions);
                chatGptDriver.Navigate().GoToUrl(CHATGPT_URL);
                Console.WriteLine("✅ Đã mở ChatGPT thành công!");
  
                Console.WriteLine("⌛ Đợi ChatGPT khởi động...");
                Thread.Sleep(5000); // Đợi 5 giây cho ChatGPT khởi động hoàn toàn
  
                // Tạo WebDriverWait để đợi các phần tử
                var wait = new WebDriverWait(chatGptDriver, TimeSpan.FromSeconds(10));
  
                // Bắt đầu vòng lặp hội thoại
                int conversationCount = 0;
                const int MAX_CONVERSATIONS = 21;
  
                while (conversationCount < MAX_CONVERSATIONS)
                {
                    conversationCount++;
                    Console.WriteLine($"\n🔄 Lượt hội thoại thứ {conversationCount}/{MAX_CONVERSATIONS}");
  
                    // Kiểm tra textarea của ChatGPT trước
                    Console.WriteLine("🔍 Kiểm tra textarea của ChatGPT...");
                    Console.WriteLine("🔍 Tìm với XPath: //p[@data-placeholder='Ask anything']");
                    IWebElement? chatGptInput = null;
                    try
                    {
                        chatGptInput = chatGptDriver.FindElement(By.XPath("//p[@data-placeholder='Ask anything']"));
                        Console.WriteLine("✅ Đã tìm thấy textarea của ChatGPT!");
                        // Hiển thị thông tin về textarea để debug
                        Console.WriteLine($"📝 Class của textarea: {chatGptInput.GetAttribute("class")}");
                        Console.WriteLine($"📝 Placeholder: {chatGptInput.GetAttribute("data-placeholder")}");
                        Console.WriteLine($"📝 Tag name: {chatGptInput.TagName}");
                        Console.WriteLine($"📝 Text hiện tại: {chatGptInput.Text}");
                        Console.WriteLine($"📝 Có hiển thị không: {chatGptInput.Displayed}");
                        Console.WriteLine($"📝 Có enable không: {chatGptInput.Enabled}");
                    }
                    catch (NoSuchElementException)
                    {
                        Console.WriteLine("❌ Không tìm thấy textarea của ChatGPT!");
                        Console.WriteLine("⚠️ Thử tìm với XPath khác...");
                        try
                        {
                            // Thử đợi một chút và tìm lại
                            Thread.Sleep(2000);
                            Console.WriteLine("🔄 Thử tìm lại sau khi đợi...");
                            chatGptInput = chatGptDriver.FindElement(By.XPath("//p[@data-placeholder='Ask anything']"));
                            Console.WriteLine("✅ Đã tìm thấy sau khi đợi!");
                        }
                        catch (NoSuchElementException)
                        {
                            Console.WriteLine("❌ Vẫn không tìm thấy phần tử nhập văn bản!");
                            Console.WriteLine("⚠️ Hiển thị source HTML để debug:");
                            try
                            {
                                var composerBackground = chatGptDriver.FindElement(By.Id("composer-background"));
                                Console.WriteLine("HTML của composer-background:");
                                Console.WriteLine(composerBackground.GetAttribute("innerHTML"));
                            }
                            catch
                            {
                                Console.WriteLine("❌ Không tìm thấy cả composer-background!");
                            }
                        }
                        Console.WriteLine("⏸️ Chương trình tạm dừng. Nhấn Enter để thử lại hoặc Ctrl+C để thoát...");
                        Console.ReadLine();
                        continue;
                    }
  
                    try
                    {
                        // Đợi và lấy câu trả lời từ ChatGPT
                        var response = wait.Until(driver => 
                            driver.FindElement(By.XPath("(//div[contains(@class, \"markdown\")])[last()]")));
  
                        if (response != null)
                        {
                            // Hiển thị thông tin về phần tử chứa câu trả lời
                            Console.WriteLine("\n🔍 Thông tin về phần tử chứa câu trả lời:");
                            Console.WriteLine($"📝 Class: {response.GetAttribute("class")}");
                            Console.WriteLine($"📝 Role: {response.GetAttribute("role")}");
                            
                            Console.WriteLine("\n🤖 ChatGPT trả lời:");
                            Console.WriteLine("------------------------------------------");
                            Console.WriteLine(response.Text);
                            Console.WriteLine("------------------------------------------\n");
                            Console.WriteLine($"📏 Độ dài câu trả lời: {response.Text.Length} ký tự");
                            
                            // Thêm log chi tiết về nội dung copy
                            Console.WriteLine("\n📋 Nội dung đã copy:");
                            Console.WriteLine("------------------------------------------");
                            foreach (var line in response.Text.Split('\n'))
                            {
                                Console.WriteLine($"| {line}");
                            }
                            Console.WriteLine("------------------------------------------");
                            
                            // Kiểm tra và log các ký tự đặc biệt
                            Console.WriteLine("\n🔍 Kiểm tra ký tự đặc biệt:");
                            Console.WriteLine($"- Có chứa xuống dòng: {response.Text.Contains("\n")}");
                            Console.WriteLine($"- Có chứa tab: {response.Text.Contains("\t")}");
                            Console.WriteLine($"- Có chứa khoảng trắng đầu/cuối: {response.Text.Trim().Length != response.Text.Length}");
                            
                            if (string.IsNullOrEmpty(response.Text))
                            {
                                Console.WriteLine("⚠️ Cảnh báo: Nội dung copy được là rỗng!");
                                
                                // Thử lấy nội dung bằng JavaScript
                                Console.WriteLine("🔄 Thử lấy nội dung bằng JavaScript...");
                                IJavaScriptExecutor js = (IJavaScriptExecutor)chatGptDriver;
                                var copiedText = (string)js.ExecuteScript("return arguments[0].textContent;", response);
                                
                                if (string.IsNullOrEmpty(copiedText))
                                {
                                    Console.WriteLine("❌ Vẫn không lấy được nội dung!");
                                    Console.WriteLine("⏸️ Chương trình tạm dừng. Nhấn Enter để thử lại hoặc Ctrl+C để thoát...");
                                    Console.ReadLine();
                                    continue;
                                }
                                else
                                {
                                    Console.WriteLine("✅ Đã lấy được nội dung bằng JavaScript!");
                                    Console.WriteLine("\n📝 Nội dung lấy được:");
                                    Console.WriteLine("------------------------------------------");
                                    Console.WriteLine(copiedText);
                                    Console.WriteLine("------------------------------------------\n");
                                }
                            }
  
                            // Thử tìm input trực tiếp
                            IWebElement? klokInput = null;
                            string klokInputXPath = "/html/body/div[1]/div[2]/div[2]/div[2]/div[1]/form/div/textarea";
                            
                            Console.WriteLine("[INFO] Looking for Klokapp input...");
                            try
                            {
                                klokInput = klokDriver.FindElement(By.XPath(klokInputXPath));
                                Console.WriteLine("[SUCCESS] Found Klokapp input!");
                            }
                            catch (NoSuchElementException)
                            {
                                Console.WriteLine("[ERROR] Input not found immediately");
                                Console.WriteLine("[INFO] Waiting 5 seconds and trying again...");
                                Thread.Sleep(5000);

                                try
                                {
                                    klokInput = klokDriver.FindElement(By.XPath(klokInputXPath));
                                    Console.WriteLine("[SUCCESS] Found input after waiting!");
                                }
                                catch (NoSuchElementException)
                                {
                                    Console.WriteLine("[ERROR] Still cannot find Klokapp input!");
                                    Console.WriteLine("[DEBUG] Current page HTML:");
                                    Console.WriteLine(klokDriver.PageSource.Substring(0, 500) + "...");
                                    throw;
                                }
                            }

                            if (klokInput != null)
                            {
                                // Clear và gửi text
                                Console.WriteLine("[INFO] Sending message to Klokapp...");
                                klokInput.Clear();
                                klokInput.SendKeys(response.Text);
                                klokInput.SendKeys(Keys.Enter);
                                Console.WriteLine("[SUCCESS] Message sent to Klokapp!");

                                // Đợi phản hồi từ Klokapp
                                Console.WriteLine("[INFO] Waiting for Klokapp response...");
                                Thread.Sleep(7000);

                                // Lấy phản hồi (cần XPath của phần response)
                                // Bạn có thể cung cấp XPath của phần response không?
                            }
                        }
                    }
                    catch (NoSuchElementException)
                    {
                        Console.WriteLine("❌ Không tìm thấy câu trả lời nào của ChatGPT");
                        break;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"⚠️ Lỗi khi tìm câu trả lời: {ex.Message}");
                        break;
                    }
  
                    // Đợi một chút trước khi bắt đầu vòng lặp mới
                    Thread.Sleep(2000);
                }
  
                Console.WriteLine($"\n✨ Đã hoàn thành {conversationCount} lượt hội thoại!");

                // Thêm đoạn code để tự động đóng chương trình
                Console.WriteLine("🔄 Đang chuẩn bị đóng chương trình...");
                isRunning = false;
                CleanupAndExit();
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Lỗi khi mở ChatGPT: {ex.Message}");
                Console.WriteLine("Đang thử lại...");
                Thread.Sleep(2000);
                
                if (chatGptDriver != null)
                {
                    try { chatGptDriver.Quit(); } catch { }
                }
                chatGptDriver = new EdgeDriver(chatGptOptions);
                chatGptDriver.Navigate().GoToUrl(CHATGPT_URL);
            }

            Console.WriteLine("✅ Đã mở tất cả các trang");

            while (isRunning)
            {
                Thread.Sleep(1000);
            }
        }
        finally
        {
            Console.WriteLine("🔄 Đang dọn dẹp và đóng các trình duyệt...");
            try 
            {
                if (chatGptDriver != null)
                {
                    chatGptDriver.Quit();
                    Console.WriteLine("✅ Đã đóng trình duyệt ChatGPT");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Lỗi khi đóng trình duyệt ChatGPT: {ex.Message}");
            }

            try 
            {
                if (klokDriver != null)
                {
                    klokDriver.Quit();
                    Console.WriteLine("✅ Đã đóng trình duyệt Kite");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Lỗi khi đóng trình duyệt Kite: {ex.Message}");
            }

            // Để chắc chắn, kill tất cả các process Chrome còn sót lại
            try 
            {
                foreach (var process in Process.GetProcessesByName("chrome"))
                {
                    process.Kill();
                }
                foreach (var process in Process.GetProcessesByName("chromedriver"))
                {
                    process.Kill();
                }
                Console.WriteLine("✅ Đã dọn dẹp tất cả các process Chrome còn sót");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Lỗi khi kill process Chrome: {ex.Message}");
            }

            Console.WriteLine("👋 Chương trình đã kết thúc");
        }
    }

    static EdgeOptions ConfigureEdgeOptions(bool isKite = true)
    {
        var options = new EdgeOptions();
        try
        {
            if (isKite)
            {
                if (!Directory.Exists(BASE_EDGE_USER_DATA_DIR))
                {
                    Console.WriteLine("[WARN] Edge profile directory not found, creating...");
                    Directory.CreateDirectory(BASE_EDGE_USER_DATA_DIR);
                }
                options.AddArgument($"--user-data-dir={BASE_EDGE_USER_DATA_DIR}");
                options.AddArgument("--profile-directory=Profile 1");
                Console.WriteLine($"[INFO] Using Klok profile: {BASE_EDGE_USER_DATA_DIR}");
            }
            else
            {
                // Đảm bảo đường dẫn ChatGPT profile tồn tại
                if (!Directory.Exists(CHATGPT_USER_DATA_DIR))
                {
                    Console.WriteLine($"[INFO] Creating ChatGPT profile at: {CHATGPT_USER_DATA_DIR}");
                    Directory.CreateDirectory(CHATGPT_USER_DATA_DIR);
                    InitializeChatGPTProfile();
                }
                else
                {
                    Console.WriteLine($"[INFO] Using existing ChatGPT profile: {CHATGPT_USER_DATA_DIR}");
                }
                options.AddArgument($"--user-data-dir={CHATGPT_USER_DATA_DIR}");
                options.AddArgument("--profile-directory=Default");
                Console.WriteLine($"[DEBUG] ChatGPT profile path: {CHATGPT_USER_DATA_DIR}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Failed to configure Edge options: {ex.Message}");
            throw;
        }

        // Các options khác giữ nguyên
        options.AddArgument("--enable-extensions");
        options.AddArgument("--disable-blink-features=AutomationControlled");
        options.AddArgument("--disable-logging");
        options.AddArgument("--log-level=3");
        options.AddArgument("--silent");
        options.AddExcludedArgument("enable-automation");
        options.AddAdditionalOption("useAutomationExtension", false);
        
        return options;
    }

    // Thêm phương thức để lấy đường dẫn Edge profile theo từng hệ điều hành
    static string GetEdgeUserDataDir()
    {
        try
        {
            // Lấy username hiện tại
            string username = Environment.UserName;
            Console.WriteLine($"[INFO] Current username: {username}");

            // Xác định hệ điều hành
            if (OperatingSystem.IsWindows())
            {
                string path = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Microsoft", "Edge", "User Data"
                );
                Console.WriteLine($"[INFO] Windows Edge profile path: {path}");
                return path;
            }
            else if (OperatingSystem.IsMacOS())
            {
                string path = Path.Combine(
                    "/Users", username,
                    "Library", "Application Support", "Microsoft Edge", "User Data"
                );
                Console.WriteLine($"[INFO] MacOS Edge profile path: {path}");
                return path;
            }
            else if (OperatingSystem.IsLinux())
            {
                string path = Path.Combine(
                    "/home", username,
                    ".config", "microsoft-edge", "User Data"
                );
                Console.WriteLine($"[INFO] Linux Edge profile path: {path}");
                return path;
            }
            else
            {
                throw new PlatformNotSupportedException("Unsupported operating system");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Failed to get Edge profile path: {ex.Message}");
            throw;
        }
    }

    static void HandleMetamask(IWebDriver driver)
    {
        try
        {
            Console.WriteLine("Doi Metamask (timeout: 5 phut)...");
            DateTime endTime = DateTime.Now.AddMinutes(5);
            string mainWindow = driver.CurrentWindowHandle;
            string? metamaskWindow = null;

            // Tìm cửa sổ Metamask đầu tiên
            while (DateTime.Now < endTime && metamaskWindow == null && isRunning)
            {
                var initialWindows = driver.WindowHandles;
                Console.WriteLine($"Dang kiem tra {initialWindows.Count} cua so...");

                foreach (string currentWindow in initialWindows)
                {
                    if (!isRunning) return;

                    driver.SwitchTo().Window(currentWindow);
                    string currentTitle = driver.Title;
                    Console.WriteLine($"Kiem tra cua so: {currentTitle}");

                    if (currentTitle == "MetaMask")
                    {
                        Console.WriteLine("Da tim thay cua so MetaMask!");
                        metamaskWindow = currentWindow;
                        
                        // Đợi 2 giây cho MetaMask load
                        Thread.Sleep(2000);
                        
                        // Nhập mật khẩu và unlock
                        Console.WriteLine("Nhap mat khau...");
                        var actions = new Actions(driver);
                        actions.SendKeys(METAMASK_PASSWORD).Perform();
                        Thread.Sleep(1000);
                        
                        Console.WriteLine("Nhan Enter de unlock...");
                        actions.SendKeys(Keys.Enter).Perform();
                        
                        // Đợi 5 giây sau khi unlock
                        Console.WriteLine("Doi 5 giay sau khi unlock...");
                        Thread.Sleep(5000);

                        // Trong phần tìm lại cửa sổ Metamask sau khi unlock
                        Console.WriteLine("Tim lai cua so Metamask sau khi unlock...");
                        try 
                        {
                            string metamaskMainWindow = driver.CurrentWindowHandle;
                            
                            // Đợi một chút cho các cửa sổ mới xuất hiện
                            Thread.Sleep(2000);

                            foreach (string handle in driver.WindowHandles)
                            {
                                if (handle != metamaskMainWindow)
                                {
                                    Console.WriteLine("Chuyen sang cua so moi...");
                                    driver.SwitchTo().Window(handle);
                                    Console.WriteLine($"Tieu de cua so: {driver.Title}");
                                    
                                    if (driver.Title == "MetaMask")
                                    {
                                        Console.WriteLine("Da tim thay cua so MetaMask!");
                                        
                                        try
                                        {
                                            // Đợi lâu hơn cho UI load hoàn toàn
                                            Console.WriteLine("Doi 3 giay cho UI load...");
                                            Thread.Sleep(3000);
                                            
                                            // Thử nhiều cách tìm nút
                            IWebElement? connectButton = null;
                            
                            // Cách 1: Tìm bằng data-testid
                                            Console.WriteLine("\nCach 1: Tim bang data-testid...");
                            try
                            {
                                                connectButton = driver.FindElement(By.CssSelector("[data-testid='confirm-btn']"));
                                                Console.WriteLine("-> Tim thay nut bang data-testid!");
                            }
                                            catch (Exception ex) 
                            {
                                                Console.WriteLine($"-> Khong tim thay: {ex.Message}");
                            }

                                            // Cách 2: Tìm bằng XPath text
                            if (connectButton == null)
                            {
                                                Console.WriteLine("\nCach 2: Tim bang XPath text...");
                                try
                                {
                                                    connectButton = driver.FindElement(By.XPath("//button[contains(text(), 'Connect')]"));
                                                    Console.WriteLine("-> Tim thay nut bang XPath!");
                                }
                                                catch (Exception ex)
                                {
                                                    Console.WriteLine($"-> Khong tim thay: {ex.Message}");
                                }
                            }

                                            // Cách 3: Tìm tất cả button và kiểm tra text
                            if (connectButton == null)
                            {
                                                Console.WriteLine("\nCach 3: Tim trong tat ca cac nut...");
                                                try
                                                {
                                                    var buttons = driver.FindElements(By.TagName("button"));
                                                    Console.WriteLine($"-> Tim thay {buttons.Count} nut tren trang");
                                                    
                                                    Console.WriteLine("Danh sach nut:");
                                                    foreach (var button in buttons)
                                                    {
                                                        try
                                                        {
                                                            string buttonText = button.Text;
                                                            string buttonClass = button.GetAttribute("class");
                                                            string buttonTestId = button.GetAttribute("data-testid");
                                                            Console.WriteLine($"- Text: '{buttonText}'");
                                                            Console.WriteLine($"  Class: {buttonClass}");
                                                            Console.WriteLine($"  Data-testid: {buttonTestId}");
                                                            Console.WriteLine($"  Displayed: {button.Displayed}");
                                                            Console.WriteLine($"  Enabled: {button.Enabled}\n");

                                                            if (buttonText.Contains("Connect"))
                                                            {
                                                                connectButton = button;
                                                                Console.WriteLine("-> Tim thay nut Connect!");
                                                                break;
                                                            }
                                                        }
                                                        catch (Exception ex)
                                                        {
                                                            Console.WriteLine($"Loi khi doc thong tin nut: {ex.Message}");
                                                        }
                                                    }
                                                }
                                                catch (Exception ex)
                                                {
                                                    Console.WriteLine($"-> Loi khi tim nut: {ex.Message}");
                                                }
                                            }

                                            // Nếu tìm thấy nút, thử click
                            if (connectButton != null)
                            {
                                                Console.WriteLine("\nKET QUA: Da tim thay nut Connect!");
                                                Console.WriteLine($"Text: {connectButton.Text}");
                                                Console.WriteLine($"Class: {connectButton.GetAttribute("class")}");
                                                Console.WriteLine($"Data-testid: {connectButton.GetAttribute("data-testid")}");
                                                Console.WriteLine($"Displayed: {connectButton.Displayed}");
                                                Console.WriteLine($"Enabled: {connectButton.Enabled}");
                                                
                                                // Đợi một chút và thử click
                                                Thread.Sleep(1000);
                                
                                if (connectButton.Displayed && connectButton.Enabled)
                                {
                                                    Console.WriteLine("\nClick vao nut Connect...");
                                    connectButton.Click();
                                                    Console.WriteLine("Da click nut Connect!");
                                                    Thread.Sleep(2000);
                                }
                                else
                                {
                                                    Console.WriteLine("\nNut Connect khong the click!");
                                }
                            }
                            else
                            {
                                                Console.WriteLine("\nKET QUA: Khong tim thay nut Connect bang bat ky cach nao!");
                            }
                        }
                        catch (Exception ex)
                        {
                                            Console.WriteLine($"\nLoi khi tim hoac click nut: {ex.Message}");
                        }

                        // Chuyển về cửa sổ chính
                                        Console.WriteLine("\nChuyen ve cua so chinh");
                                        driver.SwitchTo().Window(metamaskMainWindow);
                return;
                                    }
                                }
                            }

                            Console.WriteLine("Khong tim thay cua so MetaMask!");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Loi khi xu ly cua so MetaMask: {ex.Message}");
                        }
                    }
                }

                if (metamaskWindow == null && isRunning)
                {
                    Thread.Sleep(3000);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Loi khi xu ly Metamask: {ex.Message}");
            if (isRunning)
            {
                Console.WriteLine("Chuong trinh tam dung. Nhan Enter de thu lai hoac Ctrl+C de thoat...");
            Console.ReadLine();
            if (isRunning) HandleMetamask(driver);
            }
        }
    }

    static void KillAllEdgeProcesses()
    {
        try
        {
            Console.WriteLine("🔍 Đang kiểm tra và đóng các tiến trình Edge...");
            
            foreach (var process in Process.GetProcessesByName("msedgedriver"))
            {
                try
                {
                    process.Kill();
                }
                catch { }
            }

            foreach (var process in Process.GetProcessesByName("msedge"))
            {
                try
                {
                    process.Kill();
                }
                catch { }
            }

            Console.WriteLine("✅ Đã đóng tất cả các tiến trình Edge");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Lỗi khi đóng tiến trình Edge: {ex.Message}");
        }
    }

    static void CleanupAndExit()
    {
        Console.WriteLine("\n🛑 Đang dừng chương trình...");
        
        try
        {
            if (klokDriver != null)
            {
                klokDriver.Quit();
                klokDriver = null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Lỗi khi đóng Kite: {ex.Message}");
        }

        try
        {
            if (chatGptDriver != null)
            {
                chatGptDriver.Quit();
                chatGptDriver = null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Lỗi khi đóng ChatGPT: {ex.Message}");
        }

        KillAllEdgeProcesses();
        Console.WriteLine("✅ Đã đóng tất cả trình duyệt.");
    }

    static IWebDriver? StartEdgeWithProfile(string profileName, string userDataDir)
    {
        try
        {
            KillAllEdgeProcesses();
            Thread.Sleep(2000);

            Console.WriteLine($"📁 Sử dụng profile {profileName} từ {userDataDir}");

            var options = new EdgeOptions();
            options.AddArgument($"--user-data-dir={userDataDir}");
            options.AddArgument($"--profile-directory={profileName}");
            
            // In ra đường dẫn thực tế được sử dụng
            Console.WriteLine($"🔍 Sử dụng user data dir: {userDataDir}");
            Console.WriteLine($"🔍 Sử dụng profile: {profileName}");

            options.AddArgument("--start-maximized");
            options.AddArgument("--no-first-run");
            options.AddArgument("--no-default-browser-check");
            options.AddArgument("--password-store=basic");
            options.AddArgument("--disable-popup-blocking");
            options.AddArgument("--disable-blink-features=AutomationControlled");
            options.AddExcludedArgument("enable-automation");
            options.AddAdditionalOption("useAutomationExtension", false);
            
            var driver = new EdgeDriver(options);

            return driver;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Lỗi khi mở {profileName}: {ex.Message}");
            return null;
        }
    }

    static void InitializeEdgeProfiles()
    {
        try
        {
            Console.WriteLine("[INFO] Checking Edge profiles...");

            // Kiểm tra và tạo thư mục profile cho Kite
            if (!Directory.Exists(BASE_EDGE_USER_DATA_DIR))
            {
                Console.WriteLine("[INFO] Creating base Edge profile directory...");
                Directory.CreateDirectory(BASE_EDGE_USER_DATA_DIR);
                
                // Copy các file cấu hình cơ bản từ profile mặc định nếu có
                string defaultProfilePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Microsoft", "Edge", "User Data"
                );

                if (Directory.Exists(defaultProfilePath))
                {
                    Console.WriteLine("[INFO] Copying default profile settings...");
                    CopyProfileFiles(defaultProfilePath, BASE_EDGE_USER_DATA_DIR);
                }
            }

            // Kiểm tra và tạo Profile 1 cho Kite
            string kiteProfilePath = Path.Combine(BASE_EDGE_USER_DATA_DIR, "Profile 1");
            if (!Directory.Exists(kiteProfilePath))
            {
                Console.WriteLine("[INFO] Creating Kite profile directory...");
                Directory.CreateDirectory(kiteProfilePath);
                
                // Tạo file Preferences cơ bản cho Profile 1
                CreateDefaultPreferences(kiteProfilePath, "Kite Profile");
            }

            // Kiểm tra và tạo profile cho ChatGPT
            if (!Directory.Exists(CHATGPT_USER_DATA_DIR))
            {
                Console.WriteLine("[INFO] Creating ChatGPT profile directory...");
                Directory.CreateDirectory(CHATGPT_USER_DATA_DIR);
                
                // Tạo file Preferences cơ bản cho ChatGPT
                CreateDefaultPreferences(Path.Combine(CHATGPT_USER_DATA_DIR, "Default"), "ChatGPT Profile");
            }

            Console.WriteLine("[SUCCESS] Edge profiles initialized successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Failed to initialize profiles: {ex.Message}");
            throw;
        }
    }

    static void CopyProfileFiles(string sourcePath, string targetPath)
    {
        try
        {
            // Danh sách các file cấu hình cần copy
            string[] configFiles = {
                "Local State",
                "Preferences",
                "Secure Preferences"
            };

            foreach (string file in configFiles)
            {
                string sourceFile = Path.Combine(sourcePath, file);
                string targetFile = Path.Combine(targetPath, file);

                if (File.Exists(sourceFile) && !File.Exists(targetFile))
                {
                    File.Copy(sourceFile, targetFile);
                    Console.WriteLine($"[INFO] Copied {file}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WARN] Error copying profile files: {ex.Message}");
        }
    }

    static void CreateDefaultPreferences(string profilePath, string profileName)
    {
        try
        {
            Directory.CreateDirectory(profilePath);

            // Tạo file Preferences với cấu hình cơ bản
            var preferences = new
            {
                profile = new
                {
                    name = profileName,
                    exit_type = "Normal",
                    exited_cleanly = true
                },
                browser = new
                {
                    enabled_labs_experiments = new string[] { },
                    has_seen_welcome_page = true
                },
                extensions = new
                {
                    settings = new { }
                }
            };

            string preferencesPath = Path.Combine(profilePath, "Preferences");
            File.WriteAllText(preferencesPath, JsonSerializer.Serialize(preferences));
            Console.WriteLine($"[INFO] Created Preferences for {profileName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WARN] Error creating preferences: {ex.Message}");
        }
    }

    // Thêm hàm mới để xử lý quy trình đăng nhập Klokapp
    static void HandleKlokLogin(IWebDriver driver)
    {
        try
        {
            // Tìm và click nút Connect Wallet
            string buttonXPath = "/html/body/div[1]/div/div[4]/button[2]";
            Console.WriteLine($"Dang tim nut Connect Wallet voi XPath: {buttonXPath}");
            
            IWebElement? button = null;
            try 
            {
                button = driver.FindElement(By.XPath(buttonXPath));
                if (button != null && button.Displayed && button.Enabled)
                {
                    button.Click();
                    Console.WriteLine("Da click nut Connect Wallet");
                    Thread.Sleep(2000);

                    // Tìm và click nút Metamask trong popup
                    string metamaskButtonXPath = "//*[@id=\"__CONNECTKIT__\"]/div/div/div/div[2]/div[2]/div[3]/div/div/div/div[1]/div[1]/div/button[1]/span";
                    Console.WriteLine("Dang tim nut Metamask trong popup...");

                    IWebElement? metamaskButton = null;
                    try
                    {
                        metamaskButton = driver.FindElement(By.XPath(metamaskButtonXPath));
                        if (metamaskButton != null && metamaskButton.Displayed && metamaskButton.Enabled)
                        {
                            Console.WriteLine("Click vao nut Metamask...");
                            metamaskButton.Click();
                            Console.WriteLine("Da click nut Metamask!");
                            Thread.Sleep(2000);

                            // Xử lý Metamask popup
                            bool connectSuccess = HandleMetamaskConnect(driver);
                            
                            // Chỉ tìm nút Sign In nếu đã connect thành công
                            if (connectSuccess)
                            {
                                Thread.Sleep(3000); // Đợi sau khi connect
                                
                                try 
                                {
                                    string signInButtonXPath = "/html/body/div[1]/div/div[4]/button";
                                    Console.WriteLine("Tim nut Sign In...");
                                    var signInButton = driver.FindElement(By.XPath(signInButtonXPath));
                                    
                                    if (signInButton != null && signInButton.Displayed && signInButton.Enabled)
                                    {
                                        Console.WriteLine("Click nut Sign In...");
                                        signInButton.Click();
                                        Console.WriteLine("Da click nut Sign In!");
                                        Thread.Sleep(3000);

                                        // Sau khi click Sign In, đợi và xử lý cửa sổ Metamask mới
                                        Console.WriteLine("Doi cua so Metamask moi xuat hien...");
                                        Thread.Sleep(3000);

                                        // Tìm và xử lý cửa sổ Metamask mới
                                        string mainWindow = driver.CurrentWindowHandle;
                                        bool foundMetamaskWindow = false;

                                        foreach (string handle in driver.WindowHandles)
                                        {
                                            if (handle != mainWindow)
                                            {
                                                driver.SwitchTo().Window(handle);
                                                if (driver.Title == "MetaMask")
                                                {
                                                    Console.WriteLine("Da tim thay cua so Metamask moi!");
                                                    foundMetamaskWindow = true;

                                                    // Tab 7 lần để đến nút Confirm
                                                    Console.WriteLine("\nTab 7 lan den nut Confirm...");
                                                    var actions = new Actions(driver);
                                                    for (int i = 0; i < 7; i++)
                                                    {
                                                        actions.SendKeys(Keys.Tab).Perform();
                                                        Thread.Sleep(500);
                                                        Console.WriteLine($"Tab lan {i + 1}");
                                                    }

                                                    // Click nút Confirm
                                                    Console.WriteLine("Click nut Confirm...");
                                                    actions.SendKeys(Keys.Enter).Perform();
                                                    Console.WriteLine("Da click nut Confirm!");
                                                    Thread.Sleep(2000);

                                                    // Chuyển về cửa sổ chính
                                                    driver.SwitchTo().Window(mainWindow);
                                                    break;
                                                }
                                            }
                                        }

                                        if (!foundMetamaskWindow)
                                        {
                                            Console.WriteLine("Khong tim thay cua so Metamask moi!");
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Loi khi tim hoac click nut Sign In: {ex.Message}");
                                }
                            }
                            else
                            {
                                Console.WriteLine("Khong tim nut Sign In vi chua connect thanh cong!");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Loi khi tim nut Metamask: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Loi khi tim nut Connect Wallet: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Loi trong qua trinh dang nhap: {ex.Message}");
        }
    }

    // Tách riêng phần xử lý Metamask connect
    static bool HandleMetamaskConnect(IWebDriver driver)
    {
        try
        {
            Console.WriteLine("Tim lai cua so Metamask sau khi unlock...");
            string metamaskMainWindow = driver.CurrentWindowHandle;
            Thread.Sleep(2000);

            foreach (string handle in driver.WindowHandles)
            {
                if (handle != metamaskMainWindow)
                {
                    Console.WriteLine("Chuyen sang cua so moi...");
                    driver.SwitchTo().Window(handle);
                    Console.WriteLine($"Tieu de cua so: {driver.Title}");
                    
                    if (driver.Title == "MetaMask")
                    {
                        Console.WriteLine("Da tim thay cua so MetaMask!");
                        
                        // Nhập mật khẩu và unlock
                        Console.WriteLine("Nhap mat khau...");
                        var actions = new Actions(driver);
                        actions.SendKeys(METAMASK_PASSWORD).Perform();
                        Thread.Sleep(1000);
                        
                        Console.WriteLine("Nhan Enter de unlock...");
                        actions.SendKeys(Keys.Enter).Perform();
                        
                        // Đợi sau khi unlock
                        Console.WriteLine("Doi 5 giay sau khi unlock...");
                        Thread.Sleep(5000);

                        // Tab 5 lần để đến nút Connect
                        Console.WriteLine("\nTab 5 lan den nut Connect...");
                        for (int i = 0; i < 5; i++)
                        {
                            actions.SendKeys(Keys.Tab).Perform();
                            Thread.Sleep(500);
                            Console.WriteLine($"Tab lan {i + 1}");
                        }

                        // Click nút Connect
                        Console.WriteLine("Click nut Connect...");
                        actions.SendKeys(Keys.Enter).Perform();
                        Console.WriteLine("Da click nut Connect!");
                        Thread.Sleep(2000);

                        // Chuyển về cửa sổ chính
                        driver.SwitchTo().Window(metamaskMainWindow);
                        return true;
                    }
                }
            }
            
            Console.WriteLine("Khong tim thay cua so MetaMask!");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Loi khi xu ly Metamask connect: {ex.Message}");
            return false;
        }
    }

    // Hoặc tách thành hàm riêng để tái sử dụng
    static bool HandleMetamaskConfirm(IWebDriver driver)
    {
        try
        {
            Console.WriteLine("Tim cua so Metamask de Confirm...");
            string mainWindow = driver.CurrentWindowHandle;
            Thread.Sleep(2000);

            foreach (string handle in driver.WindowHandles)
            {
                if (handle != mainWindow)
                {
                    driver.SwitchTo().Window(handle);
                    Console.WriteLine($"Kiem tra cua so: {driver.Title}");
                    
                    if (driver.Title == "MetaMask")
                    {
                        Console.WriteLine("Da tim thay cua so MetaMask!");
                        Thread.Sleep(1000); // Đợi UI load

                        // Tab 7 lần để đến nút Confirm
                        Console.WriteLine("\nTab 7 lan den nut Confirm...");
                        var actions = new Actions(driver);
                        for (int i = 0; i < 7; i++)
                        {
                            actions.SendKeys(Keys.Tab).Perform();
                            Thread.Sleep(500);
                            Console.WriteLine($"Tab lan {i + 1}");
                        }

                        // Click nút Confirm
                        Console.WriteLine("Click nut Confirm...");
                        actions.SendKeys(Keys.Enter).Perform();
                        Console.WriteLine("Da click nut Confirm!");
                        Thread.Sleep(2000);

                        // Chuyển về cửa sổ chính
                        driver.SwitchTo().Window(mainWindow);
                        return true;
                    }
                }
            }
            
            Console.WriteLine("Khong tim thay cua so MetaMask!");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Loi khi xu ly Metamask confirm: {ex.Message}");
            return false;
        }
    }

    static void InitializeChatGPTProfile()
    {
        try
        {
            Directory.CreateDirectory(CHATGPT_USER_DATA_DIR);
            var defaultProfilePath = Path.Combine(CHATGPT_USER_DATA_DIR, "Default");
            Directory.CreateDirectory(defaultProfilePath);

            // Copy các file cấu hình từ profile gốc chỉ khi tạo mới
            CopyProfileFiles(Path.Combine(BASE_EDGE_USER_DATA_DIR, "Default"), defaultProfilePath);
            
            Console.WriteLine("[SUCCESS] Created new ChatGPT profile successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Failed to initialize ChatGPT profile: {ex.Message}");
            throw;
        }
    }
}
