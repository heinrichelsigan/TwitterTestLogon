using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Collections.Specialized;
using System.Net;
using System.IO;
using System.Web;

namespace TwitterTestLogon
{
    public partial class TweetLogin : Form
    {
        const string AUTHURL = "https://twitter.com/sessions";

        public TweetLogin()
        {
            InitializeComponent();
            this.textUsername.Text = "twitterEmail@yourdomain.com";
            // this.textPassword.Text = "";
        }


        /// <summary>
        /// event that is fired, when buttonLogin clicked
        /// </summary>
        /// <param name="sender">sender, that fires event</param>
        /// <param name="e">EventArgs e</param>
        private void buttonLogin_Click(object sender, EventArgs e)
        {
            this.textOutput.Clear();
            this.textOutput.Text = string.Format(
                "Posting Username \'{0}\' Password \'{1}\' in {2}\r\n\n",
                this.textUsername.Text, this.textPassword.Text, AUTHURL);

            // create Dictionary<string, string> postParams with default parameters to post for twitter login
            Dictionary<string, string> postParams = new Dictionary<string, string>();
            postParams.Add("session[username_or_email]", textUsername.Text);
            postParams.Add("session[password]", textPassword.Text);
            postParams.Add("remember_me", "0");
            postParams.Add("redirect_after_login", "/");
            postParams.Add("return_to_ssl", "false");
            // postParams.Add("scribe_log", string.Empty);


            // get authentication Parameters from twitter Login via HttpPostLogin(AUTHURL, postParams);
            Dictionary<string, string> authTwitterParams = this.HttpPostLogin(AUTHURL, postParams);
            // write authentication Parameters to TextBox textOutput
            this.textOutput.Text += "\r\nTwitter Login POSTING via HttpWebRequest: \r\n";
            foreach (string ikey in authTwitterParams.Keys)
            {
                string ivalue =  authTwitterParams[ikey];
                this.textOutput.Text += ikey + " \t= "
                    + authTwitterParams[ikey] + "\r\n";
                
                try
                {
                    // add 4 authentication twitter parameters to postParams
                    if (ikey.StartsWith("Set-Cookie"))
                    {
                        string guest_id = HttpUtility.UrlDecode(ParseTwitterSession(ivalue, "guest_id"));
                        postParams.Add("guest_id", guest_id);
                        this.textOutput.Text += "guest_id" + " \t= " + guest_id + "\r\n";

                        string _twitter_sess = ParseTwitterSession(ivalue, "_twitter_sess");
                        postParams.Add("_twitter_sess", _twitter_sess);
                        this.textOutput.Text += "_twitter_sess" + " \t= " + _twitter_sess + "\r\n";

                        string ct0 = ParseTwitterSession(ivalue, "ct0");
                        postParams.Add("ct0", ct0);
                        this.textOutput.Text += "ct0" + " \t= " + ct0 + "\r\n";

                        string tdomain  = ParseTwitterSession(ivalue, "Domain");
                        postParams.Add("Domain", tdomain);
                        this.textOutput.Text += "Domain" + " \t= " + tdomain + "\r\n";
                    }   
                }
                catch (Exception ex)
                {
                    this.textOutput.Text += "\r\nException: " + ex.ToString() + "\r\n";
                }
            }

            // pass postParams from Dictionary<string, string> to NameValueCollection formData 
            NameValueCollection formData = new NameValueCollection();
            foreach (string pkey in postParams.Keys) { formData[pkey] = postParams[pkey]; }

            // get authentication Parameters from twitter Login via WebClientLoginTwitter(AUTHURL, formData);
            Dictionary<string, string> gotAuthParams = this.WebClientLoginTwitter(AUTHURL, formData);
            // write authentication Parameters to TextBox textOutput
            this.textOutput.Text += "\r\nTwitter Login POSTING via WebClient: \r\n";
            foreach (string ikey in gotAuthParams.Keys)
            {
                this.textOutput.Text += ikey + " \t= "
                        + gotAuthParams[ikey] + "\r\n";
            }            

        }

        

        /// <summary>
        /// twitter login test
        /// by using System.Net.HttpWebRequest/HttpWebResponse class
        /// </summary>
        /// <param name="authUrl">twitter session url</param>
        /// <param name="postParameters">parameter collection, that will be POSTed urlencoded to requested authUrl</param>
        /// <returns>Dictionary<string, string> authParams from response Headers and authenticity_token from response body</returns>
        protected Dictionary<string, string> HttpPostLogin(string authUrl, Dictionary<string, string> postParameters)
        {
            string postData = "";
            // add all postParameters as an UrlEncoded string
            foreach (string key in postParameters.Keys)
            {
                postData += HttpUtility.UrlEncode(key) + "="
                      + HttpUtility.UrlEncode(postParameters[key]) + "&";
            }
            // get postData as byte[]
            byte[] data = Encoding.ASCII.GetBytes(postData);

            // create HttpWebRequest via authUrl // set POST Method, ContentType, ContentLength
            HttpWebRequest myHttpWebRequest = (HttpWebRequest)HttpWebRequest.Create(authUrl); 
            myHttpWebRequest.Method = "POST";
            myHttpWebRequest.ContentType = "application/x-www-form-urlencoded";
            myHttpWebRequest.ContentLength = data.Length;

            // Write postParameters as byte[] to RequestStream of created HttpWebRequest
            Stream requestStream = myHttpWebRequest.GetRequestStream();
            requestStream.Write(data, 0, data.Length);
            requestStream.Close();

            // get HttpWebResponse now from HttpWebRequest
            HttpWebResponse myHttpWebResponse = (HttpWebResponse)myHttpWebRequest.GetResponse();

            // create twitterResponseHeaders Dictionary<string, string> 
            Dictionary<string, string> twitterResponseHeaders = new Dictionary<string, string>();

            // add key values from HttpWebResponse.Headers to twitterResponseHeaders Dictionary<string, string> 
            foreach (string hkey in myHttpWebResponse.Headers.AllKeys)
            {
                string hvalue = myHttpWebResponse.Headers[hkey];
                twitterResponseHeaders.Add(hkey, hvalue);
            }

            // get ResponseStream from HttpWebResponsem, read Content of ResponseStream and store it in string pageContent
            Stream responseStream = myHttpWebResponse.GetResponseStream();
            StreamReader myStreamReader = new StreamReader(responseStream, Encoding.Default);
            string pageContent = myStreamReader.ReadToEnd();
            myStreamReader.Close();
            responseStream.Close();

            // close HttpWebResponse received from HttpWebRequest
            myHttpWebResponse.Close();

            // parse authenticity_token hidden form value from received pageContent
            string authenticity_token = ParseTwitterAuthValue(ref pageContent, "authenticity_token");
            // and add it to twitterResponseHeaders Dictionary<string, string> 
            twitterResponseHeaders.Add("authenticity_token", authenticity_token);

            return twitterResponseHeaders; // pageContent;
        }


        /// <summary>
        /// twitter login test
        /// using System.Net.WebClient class
        /// </summary>
        /// <param name="authUrl">twitter session url</param>
        /// <param name="authParams">parameter collection, that will be POSTed as NameValueCollection formData to requested authUrl</param>
        /// <returns>Dictionary<string, string> authParams from response Headers and authenticity_token from response body</returns>
        protected Dictionary<string, string> WebClientLoginTwitter(string authUrl, NameValueCollection formData)
        {
            WebClient webClient = new WebClient();

            // add authParams to NameValueCollection formData
           

            // POST to authUrl pairs in NameValueCollection formData via WebClient.UploadValues
            byte[] responseBytes = webClient.UploadValues(authUrl, "POST", formData);
            string result = Encoding.UTF8.GetString(responseBytes);

            Dictionary<string, string> responseHeaders = new Dictionary<string, string>();
            for (int rh = 0; rh < webClient.ResponseHeaders.Keys.Count; rh++)
            {
                string rhkey = webClient.ResponseHeaders.Keys[rh];
                string rhvalue = webClient.ResponseHeaders[rhkey];
                responseHeaders.Add(rhkey, rhvalue);
            }

            webClient.Dispose();
            
            string authResponse = ParseTwitterAuthValue(ref result, "authenticity_token");
            responseHeaders.Add("authenticity_token", authResponse);

            return responseHeaders; // result;
        }


        /// <summary>
        /// Dummy html code in response parsing
        /// very quick & dirty implemented
        /// </summary>
        /// <param name="pageContent">html response from webclient or httpwebresponse</param>
        /// <returns>authentication_token</returns>
        protected static string ParseTwitterAuthValue(ref string pageContent, string parseString)
        {
            int parseStringLength = parseString.Length; 
            if (string.IsNullOrEmpty(parseString) || (parseStringLength < 2)) {
                throw new ArgumentException("ParseTwitterSession(ref string pageContent, string parseString)\r\n, parseString is null, empty or to short for parse...");
            }
            
            string authResponse = string.Empty;
            if (pageContent.IndexOf(parseString) < 0)
            {
                return authResponse;
            }
            pageContent = pageContent.Substring(
                pageContent.IndexOf(parseString) + parseStringLength);

            while (pageContent.IndexOf("value=\"") > -1)
            {
                pageContent = pageContent.Substring(
                    pageContent.IndexOf("value=") + 8
                );
                if (pageContent.IndexOf('\"') > -1)
                {
                    string valueString =
                        pageContent.Substring(
                            0,
                            pageContent.IndexOf("\"")
                        );

                    if (valueString.Length > 37)
                    {
                        authResponse = valueString;
                        break;
                    }
                }
                else
                {
                    authResponse = string.Empty;
                    break;
                }
            }
            return authResponse;
        }


        /// <summary>
        /// Dummy html code in response parsing
        /// very quick & dirty implemented
        /// </summary>
        /// <param name="pageContent">html response from webclient or httpwebresponse</param>
        /// <returns>authentication_token</returns>
        protected static string ParseTwitterSession(string contentForSearch, string parseString)
        {
            int parseStringLength = parseString.Length;
            if (string.IsNullOrEmpty(parseString) || (parseStringLength < 2))
            {
                throw new ArgumentException("ParseTwitterSession(ref string pageContent, string parseString)\r\n, parseString is null, empty or to short for parse...");
            }
            string content2Parse = contentForSearch;
            string parsedValue = string.Empty;

            if (content2Parse.IndexOf(parseString) < 0)
            {
                return parsedValue;
            }
            content2Parse = content2Parse.Substring(
                content2Parse.IndexOf(parseString) + parseStringLength);

            while (content2Parse.IndexOf("=") > -1)
            {
                content2Parse = content2Parse.Substring(
                    content2Parse.IndexOf("=") + 1
                );
                if (content2Parse.IndexOf(';') > -1)
                {
                    string valueString =
                        content2Parse.Substring(
                            0,
                            content2Parse.IndexOf(";")
                        );

                    if (valueString.Length > 1)
                    {
                        parsedValue = valueString;
                        break;
                    }
                }
                else
                {
                    parsedValue = content2Parse;
                    break;
                }
            }
            return parsedValue;
        }

    }
}
