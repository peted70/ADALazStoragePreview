using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using System.Globalization;
using Microsoft.IdentityModel.Clients.ActiveDirectory; //ADAL client library for getting the access token
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace ADALazStoragePreview
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        async Task CallContainerAsync(string token)
        {
            var http = new HttpClient();

            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await http.GetAsync("https://msalazstorage.blob.core.windows.net/models?restype=container&comp=list");

            if (!response.IsSuccessStatusCode)
            {
                return;
            }
        }

        static async Task<string> GetUserOAuthToken()
        {
            const string ResourceId = "https://storage.azure.com/";
            const string AuthInstance = "https://login.microsoftonline.com/{0}/";
            const string TenantId = "ebc9a275-9a49-4936-8704-a7b92097dafb"; // Tenant or directory ID

            // Construct the authority string from the Azure AD OAuth endpoint and the tenant ID. 
            string authority = string.Format(CultureInfo.InvariantCulture, AuthInstance, TenantId);
            AuthenticationContext authContext = new AuthenticationContext(authority);

            // Acquire an access token from Azure AD. 
            AuthenticationResult result = await authContext.AcquireTokenAsync(ResourceId,
                                                                        "ff165698-5680-441a-9935-e7e3b6f0ca4b",
                                                                        new Uri(@"urn:ietf:wg:oauth:2.0:oob"),
                                                                        new PlatformParameters(PromptBehavior.Auto, false));

            return result.AccessToken;
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            string accessToken = await GetUserOAuthToken();

            // Use the access token to create the storage credentials.
            TokenCredential tokenCredential = new TokenCredential(accessToken);
            StorageCredentials storageCredentials = new StorageCredentials(tokenCredential);

            // Create a block blob using those credentials
            CloudBlockBlob blob = new CloudBlockBlob(new Uri("https://msalazstorage.blob.core.windows.net/models/Blob1.txt"), storageCredentials);

            await blob.UploadTextAsync("Blob created by Azure AD authenticated user.");

            //await CallContainerAsync(accessToken);
        }
    }
}
