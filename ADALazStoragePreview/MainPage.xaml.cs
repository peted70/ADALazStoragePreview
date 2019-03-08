using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using System.Globalization;
using Microsoft.IdentityModel.Clients.ActiveDirectory; //ADAL client library for getting the access token
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Linq;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace ADALazStoragePreview
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private const string StorageAccountName = "<your storage account name>";
        private const string StorageContainerName = "<your container name>";
        private const string ClientId = "<your client id>";
        private const string ResourceId = "https://storage.azure.com/";
        private const string AuthInstance = "https://login.microsoftonline.com/{0}/";
        private const string TenantId = "<your tenant id>"; 
        private const string RedirectUri = @"urn:ietf:wg:oauth:2.0:oob";

        public List<string> BlobList
        { get; set; } = new List<string>();

        public MainPage()
        {
            this.InitializeComponent();
        }

        async Task CallContainerAsync(string token)
        {
            var http = new HttpClient();

            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            http.DefaultRequestHeaders.Add("x-ms-version", "2018-03-28");

            var rqstStr = $"https://{StorageAccountName}.blob.core.windows.net/{StorageContainerName}?restype=container&comp=list";
            var response = await http.GetAsync(rqstStr);
            response.EnsureSuccessStatusCode();

            var respString = await response.Content.ReadAsStringAsync();
            XDocument doc = XDocument.Parse(respString);

            var blobNameList = doc.Descendants("Blob").Select(b => b.Descendants("Name").Single().Value);
            BlobListView.Items.Clear();
            foreach (var name in blobNameList)
            {
                BlobListView.Items.Add(name);
            }
        }

        static async Task<string> AuthAsync()
        {
            // Construct the authority string from the Azure AD OAuth endpoint and the tenant ID. 
            string authority = string.Format(CultureInfo.InvariantCulture, AuthInstance, TenantId);
            AuthenticationContext authContext = new AuthenticationContext(authority);
            authContext.TokenCache.Clear();

            // Acquire an access token from Azure AD. 
            AuthenticationResult result = await authContext.AcquireTokenAsync(ResourceId,
                                                    ClientId,
                                                    new Uri(RedirectUri),
                                                    new PlatformParameters(PromptBehavior.Auto, false));

            return result.AccessToken;
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            LoadingText.Visibility = Visibility.Visible;
            ErrorText.Text = string.Empty;
            try
            {
                var accessToken = await AuthAsync();
                await CallContainerAsync(accessToken);
            }
            catch (AdalException ex)
            {
                ErrorText.Text = ex.Message;
                if (ex.InnerException != null)
                {
                    ErrorText.Text += " " + ex.InnerException.Message;
                }
                if (ex.GetBaseException() != null)
                {
                    ErrorText.Text += " " + ex.GetBaseException().Message;
                }
            }
            catch (Exception ex)
            {
                ErrorText.Text = ex.Message;
                if (ex.InnerException != null)
                {
                    ErrorText.Text += " " + ex.InnerException.Message;
                }
            }
            finally
            {
                LoadingText.Visibility = Visibility.Collapsed;
            }
        }
    }
}
