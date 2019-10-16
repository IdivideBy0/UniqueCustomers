using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using System.Drawing;
using System.Net.Mail;

public partial class Register : System.Web.UI.Page
{
    private static string custID = "";

    private static DropDownList countryDropDownList;
    private static DropDownList stateDropDownList;
    private static Label stateLabel;
    private static TextBox companyName;
    private static TextBox email;
    private static TextBox username;
    private static TextBox password;
    private static TextBox phoneNumber;
    private static RequiredFieldValidator stateRequired;
    private static bool captchaValid = false;


    protected void initialize_Controls()
    {

        // get custom controls from designer

        countryDropDownList = (DropDownList)RegisterUser.CreateUserStep.ContentTemplateContainer.FindControl("CountryDropDownList");
        stateDropDownList = (DropDownList)RegisterUser.CreateUserStep.ContentTemplateContainer.FindControl("StateDropDownList");
        stateLabel = (Label)RegisterUser.CreateUserStep.ContentTemplateContainer.FindControl("StateLabel");
        companyName = (TextBox)RegisterUser.CreateUserStep.ContentTemplateContainer.FindControl("CompanyName");
        stateRequired = (RequiredFieldValidator)RegisterUser.CreateUserStep.ContentTemplateContainer.FindControl("RequiredState");
        email = (TextBox)RegisterUser.CreateUserStep.ContentTemplateContainer.FindControl("Email");
        username = (TextBox)RegisterUser.CreateUserStep.ContentTemplateContainer.FindControl("UserName");
        password = (TextBox)RegisterUser.CreateUserStep.ContentTemplateContainer.FindControl("Password");
        phoneNumber = (TextBox)RegisterUser.CreateUserStep.ContentTemplateContainer.FindControl("PhoneNumber");

        //initialize controls

        RegisterUser.Visible = captchaValid;

        countryDropDownList.Items.Add("--Select--");
        countryDropDownList.SelectedValue.Contains("--Select--");

        stateDropDownList.Visible = false;
        stateLabel.Visible = false;


    }
    
    protected void Page_Load(object sender, EventArgs e)
    {
        initialize_Controls();
        RegisterUser.ContinueDestinationPageUrl = Request.QueryString["ReturnUrl"];
        

        if (!IsPostBack)
        {
            imgCaptcha.ImageUrl = "~/CreateCaptcha.aspx?New=1";
            
            captchaValid = false;
            initialize_Controls();
        }
    }

    protected void RegisterUser_CreatingUser(object sender, EventArgs e)
    {
        custID = custID.ToUpper();
        Session["CustID"] = custID;
        Session["CompanyName"] = companyName.Text;
        Session["Country"] = countryDropDownList.SelectedValue.ToString();
        Session["State"] = stateDropDownList.SelectedValue.ToString();
        Session["PhoneNumber"] = phoneNumber.Text;

    }

    protected void RegisterUser_CreatedUser(object sender, EventArgs e)
    {
        FormsAuthentication.SetAuthCookie(RegisterUser.UserName, false /* createPersistentCookie */);
        Session["UID"] = custID;

        SendEmail();

        string continueUrl = RegisterUser.ContinueDestinationPageUrl;
        if (String.IsNullOrEmpty(continueUrl))
        {
            
            continueUrl = "~/ThankYou.aspx?Status=Success";
        }

        Response.Redirect(continueUrl);
    }

    protected void CountryDropDownList_SelectedIndexChanged(object sender, EventArgs e)
    {

        string strCompanyName = companyName.Text;
        string selectedCountry = countryDropDownList.SelectedValue.ToString();

        if (countryDropDownList.SelectedValue.Contains("USA"))
        {
            stateLabel.Visible = true;
            stateDropDownList.Visible = true;
            if (stateRequired != null)
            {
                stateRequired.Enabled = true;
            }
        }
        else
        {
            // build the foriegn customer prefix string
            custID = ParseCode("", selectedCountry, "", strCompanyName);
        }


    }

    protected void StateDropDownList_SelectedIndexChanged(object sender, EventArgs e)
    {

        string selectedState = stateDropDownList.SelectedValue.ToString();
        string strCompanyName = companyName.Text;

        if (selectedState != "")
        {

            if (stateDropDownList.SelectedValue.Contains("IL"))
            {

                custID = ParseCode("4", "USA", selectedState, strCompanyName);
            }
            else
            {
                custID = ParseCode("2", "USA", selectedState, strCompanyName);
            }
        }
        else
        {
            stateRequired.Validate();
        }


    }

    protected void CreateUserButton_Click(object sender, EventArgs e)
    {
        GenerateCustCode();

    }

    protected void GenerateCustCode()
    {
        // Perform SQL lookup of all customers with the same prefix
        // string sqlStr = "Select * from CUSTOMERS_TABLE where CustId LIKE '" + custID + "%'";
	// Replace CUSTOMERS_TABLE with YOUR customers table.
        string sqlStr = "Select * from CUSTOMERS_TABLE where CustId LIKE @custID";
        
	using (SqlConnection Conn = new SqlConnection(System.Configuration.ConfigurationManager.ConnectionStrings["YOURConnectionString"].ConnectionString))
        {
            using (SqlDataAdapter da = new SqlDataAdapter(sqlStr, Conn.ConnectionString))
            {
                da.SelectCommand.Parameters.Add("@custID", SqlDbType.VarChar, 10).Value = custID + "%"; //wildcard for any traling numerics
                using (DataTable dt = new DataTable())
                {
                    da.Fill(dt);


                    //int rowCount = (int)dt.Rows.Count;
                    int count = 0;
                    List<KeyValuePair<string, int>> custList = new List<KeyValuePair<string, int>>();

                    foreach (DataRow dr in dt.Rows)
                    {
                        custList.Add(new KeyValuePair<string, int>(dr["CustId"].ToString(), GetIntFromID(dr["CustId"].ToString())));
                        count++;
                    }

                    // sort the list 
                    custList.Sort(Compare);

                    int temp1 = 0;
                    int temp2 = 0;
                    int availNumber = 0;
                    bool found = false;

                    if (count > 0) // loop through the list and look for a hole
                    {
                        for (int x = 0; x + 1 < count; x++)
                        {
                            
                            temp1 = custList.ElementAt(x).Value;
                            temp2 = custList.ElementAt(x + 1).Value;
                            

                            if ((temp1 + 1) != temp2)
                            {
                                availNumber = temp1 + 1;
                                
                                found = true;
                                x = count; // exit the loop
                            }

                        }
                    }
                    if (!found && count > 0)
                    {
                        availNumber = (custList.ElementAt(count - 1).Value) + 1;
                        //Add a zero before the available number if less than 10 ex... 2PI02
                        if (availNumber < 10)
                        {
                            custID = custID + "0" + Convert.ToString(availNumber);
                        }
                        else
                        {
                            custID = custID + Convert.ToString(availNumber);
                        }
                    }
                    else
                    {
                        if (availNumber < 10)
                        {
                            custID = custID + "0" + Convert.ToString(availNumber);
                        }
                        else
                        {
                            custID = custID + Convert.ToString(availNumber);
                        }
                    }

                }
            }
        }
    }

    protected static int Compare(KeyValuePair<string, int> a, KeyValuePair<string, int> b)
    {
        return a.Value.CompareTo(b.Value);
    }

    protected string ParseCode(string pfx, string country, string state, string company)
    {
        string temp = "";


        if (country == "USA")
        {
            if (pfx == "2")
            {
                // code = 2 + first letter of company name + first letter of state
                temp = pfx + company.Substring(0, 1) + state.Substring(0, 1);

            }
            else //must be Illinois customer
            {
                // code = 4 + first two letters of the company name
                temp = pfx + company.Substring(0, 2);
            }
        }
        else
        {
            // parse first 2 letters of the company name followed by our 3 character country code

            if (company != String.Empty)
            {
                temp = company.Substring(0, 2);

                
                temp = temp + country;
            }


        }

        return temp;
    }

    protected int GetIntFromID(string str)
    {

        //clear any prefix numbers before a letter


        char[] c = str.ToCharArray();
        System.Text.StringBuilder _extractnumbers = new System.Text.StringBuilder();
        int count = 0;

        foreach (char _c in c)
        {
            ++count; // we are at the first character
            if ((Char.IsNumber(_c) == true) && (count != 1)) //skip first number
            {

                _extractnumbers.Append(_c);

            }

        }

        int value = Convert.ToInt32(_extractnumbers.ToString());
        return value;
    }

    protected void btnCaptcha_Click(object sender, EventArgs e)
    {
        imgCaptcha.ImageUrl = "~/CreateCaptcha.aspx?New=0";
        if (Session["CaptchaCode"] != null && txtCaptcha.Text == Session["CaptchaCode"].ToString())
        {

            // hide captcha stuff
            /* imgCaptcha.Visible = false;
             txtCaptcha.Visible = false;
             lblEnterCaptcha.Visible = false;
             lblMessage.Visible = false;
             btnCaptcha.Visible = false; */
            Panel1.Visible = false;

            captchaValid = true;
            //RegisterUser.Visible = true;
            initialize_Controls();
        }
        else
        {
            lblMessage.ForeColor = Color.Red;
            lblMessage.Text = "Captcha code is wrong!!";
            //RegisterUser.Visible = false;
            captchaValid = false;
            initialize_Controls();
        }
    }

    protected bool SendEmail()
    {


        string strBodyClient = String.Empty;

        string txtFrom = "YOUR EMAIL SERVER address"; // Example info@company.com
        string txtTo = email.Text;
        string txtSubject = "Thank you for registering.";

        strBodyClient = "";
        strBodyClient = "<<This is an automated email, please do not reply.>>" + Environment.NewLine + Environment.NewLine;
        strBodyClient = strBodyClient + "Thank you for registering." + System.Environment.NewLine + System.Environment.NewLine;
        strBodyClient = strBodyClient + "Here is the login information you provided:" + System.Environment.NewLine + System.Environment.NewLine;
        strBodyClient = strBodyClient + "User Name: " + username.Text +  "\r\n";
        strBodyClient = strBodyClient + "Password: " + password.Text + "\r\n";
        strBodyClient = strBodyClient + "Company Name: " + companyName.Text + System.Environment.NewLine;
        strBodyClient = strBodyClient + "Country:" + countryDropDownList.SelectedValue + System.Environment.NewLine;
        strBodyClient = strBodyClient + "State:" + " " + stateDropDownList.SelectedValue + System.Environment.NewLine;
        strBodyClient = strBodyClient + "Phone:" + phoneNumber.Text + System.Environment.NewLine;
        strBodyClient = strBodyClient + "Email: " + email.Text + System.Environment.NewLine + System.Environment.NewLine;
 

        MailMessage mailObjClient = new MailMessage(txtFrom, txtTo, txtSubject, strBodyClient);
        MailMessage mailObjHost = new MailMessage(txtFrom, "valued_recipient@company.com", "New Website Registration - Customer Code = " + custID, strBodyClient);
        mailObjHost.To.Add(""); // add additional company recipients here.
        mailObjHost.To.Add(""); 
        

        SmtpClient SMTPServer = new SmtpClient("mx.printersrepairparts.com");
        try
        {
            SMTPServer.Send(mailObjClient);
            SMTPServer.Send(mailObjHost);

        }
        catch (Exception ex)
        {
            ExceptionUtility.LogException(ex, HttpContext.Current.Request.Url.AbsoluteUri.ToString());
            ExceptionUtility.NotifySystemOps(ex);           
            return false;
        }

        return true;
    }


}