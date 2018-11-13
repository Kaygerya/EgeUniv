using log4net;
using log4net.Config;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Google.Contacts;
using Google.GData.Extensions;
using System.Threading;
using Google.Apis.Util.Store;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.GData.Client;
using Google.GData.Contacts;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Auth.OAuth2.Flows;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(Form1));

        //client secret and client public key
        string clientId = "899990018154-ni7b6dkoog9qbleu3jm57k58r5ogkqu2.apps.googleusercontent.com";
        string clientSecret = "ceraVKARO_i7TYPH30qCXODE";
        string refreshToken = "";
        TokenResponse tokenResponse = new TokenResponse();

        public Form1()
        {
            InitializeComponent();
            //authenticate at startup
            Authenticate();
        }

        public void Authenticate()
        {
            //scope
            string[] scopes = new string[] { "https://www.google.com/m8/feeds/" };     // view your basic profile info.
            try
            {
                ClientSecrets secrets = new ClientSecrets();
                secrets.ClientId = clientId;
                secrets.ClientSecret = clientSecret;
                // Use the current Google .net client library to get the Oauth2 stuff.
                UserCredential credential = GoogleWebAuthorizationBroker.AuthorizeAsync(secrets
                                                                                             , scopes
                                                                                             , "test"
                                                                                             , CancellationToken.None
                                                                                             , new FileDataStore("test")).Result;
                // Translate the Oauth permissions to something the old client libray can read
                tokenResponse = credential.Token;

                //older date shoud be renew the token.  token has 1 hour only
                if (credential.Token.IssuedUtc < DateTime.UtcNow)
                {
                    //refresh token process
                    RefreshToken(secrets, scopes);
                }
                //fetch contacts from google
                RunContactsSample();
            }
            catch (Exception ex)
            {
                //log Error
                log.Error("Authentication Error");
            }
        }

        private void RunContactsSample()
        {

            try
            {
                OAuth2Parameters parameters = new OAuth2Parameters();
                parameters.AccessToken = tokenResponse.AccessToken;
                parameters.RefreshToken = tokenResponse.RefreshToken;

                RequestSettings settings = new RequestSettings("Google contacts tutorial", parameters);
                // max contact count
                settings.PageSize = int.MaxValue;
                ContactsRequest cr = new ContactsRequest(settings);
                Feed<Contact> f = cr.GetContacts();

                DataTable table = new DataTable();
                table.Columns.Add("Name".ToString());
                table.Columns.Add("Email".ToString());
                table.Columns.Add("Phone".ToString());

                DataRow headerRow = table.NewRow();
                headerRow["Name"] = "FullName";
                headerRow["Email"] = "Email";
                headerRow["Phone"] = "Phone";

                table.Rows.Add(headerRow);

                if (f.Entries.Count() == 0)
                {  //log row count Info
                    log.Info("Contacts fetched" + " " + "0 row");
                }
                foreach (Contact c in f.Entries)
                {
                    DataRow dr = table.NewRow();
                    dr["Name"] = c.Name.FullName;
                    dr["Email"] = c.Emails.FirstOrDefault()?.Address;
                    dr["Phone"] = c.Phonenumbers.FirstOrDefault()?.Uri;
                    table.Rows.Add(dr);
                }

                ContatcsGridView.DataSource = table;
                ContatcsGridView.Columns[0].Width = 100;
                ContatcsGridView.Columns[1].Width = 150;
                ContatcsGridView.Columns[2].Width = 150;

                //log row count Info
                log.Info("Contacts fetched" + " " + table.Rows.Count);
            }
            catch (Exception a)
            {
                //log Error
                log.Error("Fetch Contatcs Error");
            }
        }

        public void RefreshToken(ClientSecrets secrets, string[] scopes)
        {
            IAuthorizationCodeFlow flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = secrets,
                Scopes = scopes
            });

            UserCredential credential = new UserCredential(flow, "me", tokenResponse);
            bool success = credential.RefreshTokenAsync(CancellationToken.None).Result;

            tokenResponse = credential.Token;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                //older date shoud be renew the token.  token has 1 hour only
                if (tokenResponse.IssuedUtc < DateTime.UtcNow)
                {
                    //reauthenticate
                    Authenticate();
                }
                OAuth2Parameters parameters = new OAuth2Parameters();
                parameters.AccessToken = tokenResponse.AccessToken;
                parameters.RefreshToken = tokenResponse.RefreshToken;

                RequestSettings settings = new RequestSettings("Google contacts tutorial", parameters);
                // max contact count
                ContactsRequest cr = new ContactsRequest(settings);

                // new entry
                Contact entry = new Contact();
                entry.Name.FullName = txtName.Text;
                EMail email = new EMail(txtEmail.Text);
                PhoneNumber phonenumber = new PhoneNumber(txtPhone.Text);
                phonenumber.Rel = "http://schemas.google.com/g/2005#work";
                entry.Phonenumbers.Add(phonenumber);

                Uri feedUri = new Uri(ContactsQuery.CreateContactsUri("default"));
                Contact createdEntry = cr.Insert<Contact>(feedUri, entry);

                lblResult.Text = "Successfully Added: " + createdEntry.Name.FullName;
                lblResult.Visible = true;
                lblResult.ForeColor = Color.Green;

                RunContactsSample();
            }
            catch (Exception ex)
            {
                lblResult.Text = ex.Message;
                lblResult.Visible = true;
                lblResult.ForeColor = Color.Red;
                log.Error(ex.Message);
            }
        }

    }
}

