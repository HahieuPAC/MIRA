using OpenQA.Selenium;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Support.UI;
using OpenQA.Selenium.Interactions;
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
    static readonly string CHATGPT_URL = "https://chatgpt.com/c/67cdbd3e-3f7c-800c-b3db-f5047e8f4634";
    static readonly string METAMASK_PASSWORD = "H@trunghj3up@c112358";
    
    // Thêm hằng số cho đường dẫn profile
    static readonly string BASE_EDGE_USER_DATA_DIR = GetEdgeUserDataDir();
    static readonly string CHATGPT_USER_DATA_DIR = Path.Combine(
        Path.GetDirectoryName(GetEdgeUserDataDir()) ?? "",
        "User Data ChatGPT"
    );

    static readonly string KLOK_INPUT_XPATH = "/html/body/div[1]/div[2]/div[2]/div[2]/div[1]/form/div/textarea";

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
                        
                        // Mở ChatGPT trong cửa sổ mới
                        Console.WriteLine("Dang mo ChatGPT...");
                        Thread.Sleep(2000);
                        
                        try
                        {
                            chatGptDriver = new EdgeDriver(chatGptOptions);
                            chatGptDriver.Navigate().GoToUrl(CHATGPT_URL);
                            Console.WriteLine("Da mo ChatGPT thanh cong!");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Loi khi mo ChatGPT: {ex.Message}");
                        }
                    }
                }
                catch (NoSuchElementException)
                {
                    Console.WriteLine("Chua dang nhap -> Tim nut Connect Wallet...");
                    // Tiếp tục code tìm nút Connect Wallet và xử lý đăng nhập
                    // ... code xử lý đăng nhập hiện tại ...
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Loi khi kiem tra trang thai dang nhap: {ex.Message}");
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
                            
                            // Đợi sau khi click
                            Thread.Sleep(2000);
                        }
                        else 
                        {
                            Console.WriteLine("Nut Sign In khong the click!");
                            Console.WriteLine($"- Hien thi: {signInButton.Displayed}");
                            Console.WriteLine($"- Kich hoat: {signInButton.Enabled}");
                        }
                    }
                }
                catch (NoSuchElementException)
                {
                    Console.WriteLine("Khong tim thay nut Sign In, thu lai sau 3 giay...");
                    Thread.Sleep(3000);
                    
                    try
                    {
                        Console.WriteLine("Tim nut Sign In lan 2...");
                        signInButton = klokDriver.FindElement(By.XPath(signInButtonXPath));
                        if (signInButton != null && signInButton.Displayed)
                        {
                            signInButton.Click();
                            Console.WriteLine("Da click nut Sign In trong lan thu 2!");
                        }
                    }
                    catch (Exception retryEx)
                    {
                        Console.WriteLine($"Khong the tim nut Sign In lan 2: {retryEx.Message}");
                        Console.WriteLine("HTML hien tai:");
                        Console.WriteLine(klokDriver.PageSource.Substring(0, 500) + "...");
                    }
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
                                Console.WriteLine("📝 HTML của composer-background:");
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
                        var lastResponse = wait.Until(driver => 
                            driver.FindElement(By.XPath("(//div[contains(@class, \"markdown\")])[last()]")));
  
                        if (lastResponse != null)
                        {
                            // Hiển thị thông tin về phần tử chứa câu trả lời
                            Console.WriteLine("\n🔍 Thông tin về phần tử chứa câu trả lời:");
                            Console.WriteLine($"📝 Class: {lastResponse.GetAttribute("class")}");
                            Console.WriteLine($"📝 Role: {lastResponse.GetAttribute("role")}");
                            
                            Console.WriteLine("\n🤖 ChatGPT trả lời:");
                            Console.WriteLine("------------------------------------------");
                            Console.WriteLine(lastResponse.Text);
                            Console.WriteLine("------------------------------------------\n");
                            Console.WriteLine($"📏 Độ dài câu trả lời: {lastResponse.Text.Length} ký tự");
                            
                            // Lưu nội dung để kiểm tra
                            string copiedText = lastResponse.Text;
                            if (string.IsNullOrEmpty(copiedText))
                            {
                                Console.WriteLine("⚠️ Cảnh báo: Nội dung copy được là rỗng!");
                                
                                // Thử lấy nội dung bằng JavaScript
                                Console.WriteLine("🔄 Thử lấy nội dung bằng JavaScript...");
                                IJavaScriptExecutor js = (IJavaScriptExecutor)chatGptDriver;
                                copiedText = (string)js.ExecuteScript("return arguments[0].textContent;", lastResponse);
                                
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
                                klokInput.SendKeys(copiedText);
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

    static EdgeOptions ConfigureEdgeOptions(bool isKlok = true)
    {
        var options = new EdgeOptions();
        try
        {
            if (isKlok)
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
                if (!Directory.Exists(CHATGPT_USER_DATA_DIR))
                {
                    Console.WriteLine("[WARN] ChatGPT profile directory not found, creating...");
                    Directory.CreateDirectory(CHATGPT_USER_DATA_DIR);
                }
                options.AddArgument($"--user-data-dir={CHATGPT_USER_DATA_DIR}");
                options.AddArgument("--profile-directory=Default");
                Console.WriteLine($"[INFO] Using ChatGPT profile: {CHATGPT_USER_DATA_DIR}");
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

                        // Tìm lại cửa sổ Metamask để click nút kết nối
                        Console.WriteLine("Tim lai cua so Metamask de ket noi...");
                        bool foundConnectButton = false;
                        var newWindows = driver.WindowHandles;

                        Console.WriteLine("Tim nut ket noi bang nhieu cach...");
                        try
                        {
                            IWebElement? connectButton = null;
                            
                            // Cách 1: Tìm bằng data-testid
                            try
                            {
                                Console.WriteLine("Thu tim nut bang data-testid...");
                                connectButton = driver.FindElement(By.CssSelector("button[data-testid='confirm-btn']"));
                                Console.WriteLine("Da tim thay nut bang data-testid!");
                            }
                            catch (NoSuchElementException)
                            {
                                Console.WriteLine("Khong tim thay nut bang data-testid");
                            }

                            // Cách 2: Tìm bằng class
                            if (connectButton == null)
                            {
                                try
                                {
                                    Console.WriteLine("Thu tim nut bang class...");
                                    connectButton = driver.FindElement(By.CssSelector("button.mm-button-primary"));
                                    Console.WriteLine("Da tim thay nut bang class!");
                                }
                                catch (NoSuchElementException)
                                {
                                    Console.WriteLine("Khong tim thay nut bang class");
                                }
                            }

                            // Cách 3: Tìm bằng text
                            if (connectButton == null)
                            {
                                try
                                {
                                    Console.WriteLine("Thu tim nut bang text...");
                                    connectButton = driver.FindElement(By.XPath("//button[contains(text(), 'Kết nối')]"));
                                    Console.WriteLine("Da tim thay nut bang text!");
                                }
                                catch (NoSuchElementException)
                                {
                                    Console.WriteLine("Khong tim thay nut bang text");
                                }
                            }

                            // Nếu tìm thấy nút bằng bất kỳ cách nào
                            if (connectButton != null)
                            {
                                Console.WriteLine("Da tim thay nut ket noi!");
                                Console.WriteLine($"Text tren nut: {connectButton.Text}");
                                Console.WriteLine($"Class cua nut: {connectButton.GetAttribute("class")}");
                                
                                if (connectButton.Displayed && connectButton.Enabled)
                                {
                                    Console.WriteLine("Click vao nut ket noi...");
                                    connectButton.Click();
                                    Console.WriteLine("Da click nut ket noi!");
                                    foundConnectButton = true;
                                    break;
                                }
                                else
                                {
                                    Console.WriteLine("Nut ket noi khong the click!");
                                    Console.WriteLine($"- Hien thi: {connectButton.Displayed}");
                                    Console.WriteLine($"- Kich hoat: {connectButton.Enabled}");
                                }
                            }
                            else
                            {
                                Console.WriteLine("Khong tim thay nut ket noi bang bat ky cach nao!");
                                // Log HTML để debug
                                Console.WriteLine("HTML hien tai:");
                                Console.WriteLine(driver.PageSource.Substring(0, 500) + "...");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Loi khi tim nut ket noi: {ex.Message}");
                        }

                        // Chuyển về cửa sổ chính
                        driver.SwitchTo().Window(mainWindow);
                        Console.WriteLine("Da chuyen ve cua so chinh");
                return;
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
}
