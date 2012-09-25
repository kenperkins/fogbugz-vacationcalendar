using System;
using System.Data;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;

using FogCreek.FogBugz;
using FogCreek.FogBugz.Database;
using FogCreek.FogBugz.Plugins;
using FogCreek.FogBugz.Plugins.Entity;
using FogCreek.FogBugz.Plugins.Api;
using FogCreek.FogBugz.Plugins.Interfaces;
using FogCreek.FogBugz.UI;

namespace VacationCalendar
{
    public class VacationCalendar : Plugin, IPluginPageDisplay, IPluginExtrasMenu, IPluginRawPageDisplay
    {
        public VacationCalendar(CPluginApi api)
            : base(api)
        {
        }
        #region Page UI Methods

        protected string GetButton(string direction)
        {
            /* Use dictionaries to create custom attributes for the standard FogBugz form
             * elements available through FogCreek.FogBugz.UI */

            Dictionary<string, string> buttonAttr = new Dictionary<string, string>();
            buttonAttr.Add("onclick", "javascript:switchMonth('" + direction + "');");

            return Forms.SubmitButton("id" + direction, direction, buttonAttr);            
        }

        protected string GetJavascript(int month, int year)
        {
            /* IMPORTANT NOTE: The plugin prefix must be preprended to every
             * argument placed in the querystring of the AJAX request. */

            string switchMonthUrl = api.Url.PluginRawPageUrl() +
                String.Format("&{0}action=switch&{0}actionToken={1}",
                api.PluginPrefix,
                api.Security.GetActionToken("switch"));

            /* When generating javascript, it's good practice to use multiline
             * string literals and nicely formatted code. This makes it easy
             * to interpret the syntax and debug on the client side. 
             * 
             * Also, note that the jquery library (v 1.3.1) is available
             * for use in javascript sent along with a page generated
             * by the IPluginPageDisplay interface.
             */

            return @"<script type='text/javascript'>
                        
                        var prefix = '" + api.PluginPrefix + @"';
                        var month = " + month.ToString() + @";
                        var year = " + year.ToString() + @";

                        function calendarFn(sHTML, status)
                        {
                            $('div#dataDiv').html(sHTML);
                        }

                        function switchMonth(direction) {
                            if (direction == 'Prev') {
                                if (month == 1) {
                                    month = 12;
                                    year = year - 1;
                                }
                                else {
                                    month = month - 1;
                                }
                            }
                            else {
                                if (month == 12) {
                                    month = 1;
                                    year = year + 1;
                                }
                                else {
                                    month = month + 1;
                                }
                            }
                            
                            var url = '" + switchMonthUrl + @"&' + prefix + 'month=' + month + '&' + prefix + 'year=' + year;      
                            jQuery.get(url, calendarFn);

                        } 
                   </script>";
        }

        public string GetPageContents(int month, int year)
        {
            StringBuilder ret = new StringBuilder();

            Dictionary<DateTime, List<String>> vacations = GetVacationSchedule(month, year);

            DateTime monthName = new DateTime(year, month, 1);
            ret.Append(@"<table cellpadding=""0"" cellspacing=""0"" border=""0"">");
            ret.Append(@"<tr>
                <td width=""150"">" + GetButton("Prev") + @"</td>
                <td width=""575"" align=""center""><h1>Vacation Calendar " + monthName.ToString("MMMM yyyy") + @"</h1></td>
                <td width=""150"" align=""right"">" + GetButton("Next") + @"</td>
            </tr>
        </table><br />");

            Table t = new Table();

            t.Width = new Unit(875, UnitType.Pixel);

            t.GridLines = GridLines.Both;
            t.CellPadding = 0;
            t.CellSpacing = 0;
            t.BorderStyle = BorderStyle.None;
            t.BorderWidth = new Unit(0);

            DateTime currentDate = new DateTime(monthName.Year, monthName.Month, 1);
            DateTime startDate = currentDate;

            TableHeaderRow header = new TableHeaderRow();
            header.Font.Name = "tahoma";
            header.Font.Size = new FontUnit("8pt");
            header.Font.Bold = true;
            header.HorizontalAlign = HorizontalAlign.Center;
            header.BackColor = System.Drawing.Color.Black;
            header.ForeColor = System.Drawing.Color.White;

            int dayMonthStarts = (int)monthName.DayOfWeek;
            int daysInMonth = DateTime.DaysInMonth(year, month);

            int remainder = 0;
            int numRows = Math.DivRem((daysInMonth + dayMonthStarts), 7, out remainder);

            if (remainder > 0)
                numRows++;

            for (int f = 0; f < 7; f++)
            {
                DayOfWeek dx = (DayOfWeek)f;
                TableCell cell = new TableCell();
                cell.Text = dx.ToString();
                cell.Height = new Unit(20, UnitType.Pixel);

                header.Cells.Add(cell);
            }

            t.Rows.Add(header);

            for (int i = 0; i < numRows; i++)
            {
                TableRow row = new TableRow();

                for (int j = 0; j < 7; j++)
                {
                    TableCell cell = new TableCell();
                    cell.Text = j.ToString();
                    cell.Height = new Unit(100, UnitType.Pixel);
                    cell.Width = new Unit(125, UnitType.Pixel);
                    cell.VerticalAlign = VerticalAlign.Top;
                    cell.BorderStyle = BorderStyle.Solid;
                    row.Cells.Add(cell);


                }

                t.Rows.Add(row);
            }

            bool bStarted = false;

            foreach (TableRow row in t.Rows)
            {
                if (row == t.Rows[0])
                    continue;

                foreach (TableCell cell in row.Cells)
                {
                    if (cell == row.Cells[0] || cell == row.Cells[6])
                    {
                        cell.BackColor = System.Drawing.Color.LightGray;
                    }

                    if (!bStarted)
                    {
                        if (cell.Text == ((int)currentDate.DayOfWeek).ToString())
                        {
                            bStarted = true;
                        }
                        else
                        {
                            cell.Text = String.Empty;
                        }
                    }

                    if (currentDate.Month != startDate.Month)
                    {
                        bStarted = false;
                        cell.Text = String.Empty;
                    }

                    if (bStarted)
                    {
                        if (currentDate != DateTime.Today)
                            cell.Text = String.Format("<div style=\"padding: 5px; background-color: silver; font-family: tahoma; font-size: 8pt; text-align: right;\">{0}</div><div style=\"padding: 5px; font-family: tahoma; font-size: 8pt; \">", currentDate.Day.ToString());
                        else
                            cell.Text = String.Format("<div style=\"padding: 5px; background-color: orange; font-family: tahoma; font-size: 8pt; text-align: right; color: black; font-weight: bold;\">{0}</div><div style=\"padding: 5px; font-family: tahoma; font-size: 8pt; \">", currentDate.Day.ToString());

                        if (vacations.ContainsKey(currentDate))
                        {
                            if (vacations[currentDate][0] == "Everyone")
                            {
                                cell.Text += String.Format("<span style=\"font-weight: bold;\">{0}</span><br />", "Everyone");
                                cell.BackColor = System.Drawing.ColorTranslator.FromHtml("#FFBBBB");
                            }
                            else
                            {
                                foreach (String s in vacations[currentDate])
                                {
                                    cell.Text += s + "<br />";
                                    cell.BackColor = System.Drawing.Color.PeachPuff;
                                }
                            }
                        }

                        cell.Text += "</div>";
                        currentDate = currentDate.AddDays(1);
                    }
                }
            }

            System.IO.TextWriter stringWriter = new System.IO.StringWriter();
            HtmlTextWriter htmlWriter = new HtmlTextWriter(stringWriter);
            t.RenderControl(htmlWriter);

            ret.Append(stringWriter.ToString());

            return ret.ToString();
        }
        #endregion

        #region Data Access Methods
        protected List<DateTime> GetPublicHolidays(int year, int month)
        {
            List<DateTime> ret = new List<DateTime>();

            int days = DateTime.DaysInMonth(year, month);

            //"api" is an instance of CPluginApi
            CSelectQuery query = api.Database.NewSelectQuery("Holiday");

            /* ignorepermissions is required for queries against FogBugz tables */
            query.IgnorePermissions = true;
            query.AddSelect("Holiday.ixPerson, dtHoliday, dtHolidayEnd");
            query.AddWhere("ixPerson = 0 AND (dtHoliday BETWEEN @startRange AND @endRange OR dtHolidayEnd BETWEEN @startRange AND @endRange)");
            query.SetParamDate("startRange", new DateTime(year, month, 1));
            query.SetParamDate("endRange", new DateTime(year, month, days));

            DataSet dataset = query.GetDataSet();
            DataRowCollection rows = dataset.Tables[0].Rows;

            for (int i = 1; i < days; i++)
            {
                DateTime current = new DateTime(year, month, i);
                for (int j = 0; j < dataset.Tables[0].Rows.Count; j++)
                {
                    DateTime start = Convert.ToDateTime(rows[j][1].ToString());
                    DateTime end = Convert.ToDateTime(rows[j][2].ToString());

                    if (current >= start && current <= end)
                        ret.Add(current);
                    
                }
            }

            return ret;
        }

        protected DataRowCollection GetRows(int year, int month)
        {
            int days = DateTime.DaysInMonth(year, month);

            //"api" is an instance of CPluginApi
            CSelectQuery query = api.Database.NewSelectQuery("Holiday");

            /* ignorepermissions is required for queries against FogBugz tables */
            query.IgnorePermissions = true;
            query.AddSelect("Holiday.ixPerson, dtHoliday, dtHolidayEnd");
            query.AddLeftJoin("Person", "Holiday.ixPerson = Person.ixPerson");
            query.AddSelect("Person.sFullName");
            query.AddWhere("dtHoliday BETWEEN @startRange AND @endRange OR dtHolidayEnd BETWEEN @startRange AND @endRange");
            query.SetParamDate("startRange", new DateTime(year, month, 1));
            query.SetParamDate("endRange", new DateTime(year, month, days));
            query.AddOrderBy("Holiday.ixPerson");

            return query.GetDataSet().Tables[0].Rows;

        }
        protected Dictionary<DateTime, List<String>> GetVacationSchedule(int month, int year)
        {
            Dictionary<DateTime, List<String>> vacations = new Dictionary<DateTime, List<string>>();

            // Figure out how many days are in the month
            int days = DateTime.DaysInMonth(year, month);

            // Get the public holidays
            List<DateTime> publicDates = GetPublicHolidays(year, month);

            // Get the DataRowCollection containing the specific people on dates
            DataRowCollection rows = GetRows(year, month);

            // Cycle through the days of the month
            for (int day = 1; day < days; day++)
            {
                DateTime current = new DateTime(year, month, day);
                List<string> listOfPeople = new List<string>();

                // Public holiday?
                if (publicDates.Contains(current))
                {
                    listOfPeople.Add("Everyone");
                }
                // If we're not a public holiday, lets get the people for the date
                else
                {
                    // Column 0 is ID
                    // Column 1 is Start Date
                    // Column 2 is End Date
                    // Column 3 is Name

                    // Cycle through the people looking for the the current date
                    for (int row = 0; row < rows.Count; row++)
                    {
                        DateTime start = Convert.ToDateTime(rows[row][1].ToString());
                        DateTime end = Convert.ToDateTime(rows[row][2].ToString());

                        // If our date is between the dates for this person, add it to our set for today
                        if (current >= start && current <= end)
                        {
                            listOfPeople.Add(rows[row][3].ToString());
                        }
                    }
                }

                // If we've got anyone in our people list, add it to the vacations
                if (listOfPeople.Count > 0)
                    vacations.Add(current, listOfPeople);
                
            }

            return vacations;

        }
        #endregion
        
        #region IPluginExtrasMenu Members

        public CNavMenuLink[] ExtrasMenuLinks()
        {
            return new CNavMenuLink[] { new CNavMenuLink("Vacation Calendar", api.Url.PluginPageUrl()) };
        }

        #endregion

        #region IPluginPageDisplay Members

        public string PageDisplay()
        {
            StringBuilder ret = new StringBuilder();

            int year = DateTime.Now.Year;
            int month = DateTime.Now.Month;

            ret.Append(GetJavascript(month, year));
            ret.Append(@"<div id=""dataDiv"">");
            ret.Append(GetPageContents(month, year));
            ret.Append(@"</div>");

            return ret.ToString();
        }

        public PermissionLevel PageVisibility()
        {
            return PermissionLevel.Public;
        }

        #endregion

        #region IPluginRawPageDisplay Members

        public string RawPageDisplay()
        {
            if (api.Request[api.AddPluginPrefix("action")] != null &&
                Convert.ToString(api.Request[api.AddPluginPrefix("action")]) == "switch")
            {
                // make sure the request includes a valid action token
                if ((api.Request[api.AddPluginPrefix("actionToken")] == null) ||
                    !api.Security.ValidateActionToken(api.Request[api.AddPluginPrefix("actionToken")],
                                                          "switch"))
                {
                    return "Action token invalid";
                }
                else
                {
                    int month = 0;
                    int year = 0;

                    // Do our page here
                    if (api.Request[api.AddPluginPrefix("month")] == null ||
                        api.Request[api.AddPluginPrefix("year")] == null)
                    {
                        month = DateTime.Now.Month;
                        year = DateTime.Now.Year;
                    }
                    else
                    {
                        month = Convert.ToInt32(api.Request[api.AddPluginPrefix("month")]);
                        year = Convert.ToInt32(api.Request[api.AddPluginPrefix("year")]);
                    }
                    

                    return GetPageContents(month, year);
                }

            }
            else return String.Format("command '{0}' not recognized", api.AddPluginPrefix("action"));
        }

        public PermissionLevel RawPageVisibility()
        {
            return PermissionLevel.Public;
        }

        #endregion
    }
}
