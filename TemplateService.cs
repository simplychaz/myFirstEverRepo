using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
namespace RCBC.BulkTransaction.API.Helpers {
    /// <summary>
    /// Template Service
    /// </summary>
    public class TemplateService {
        //----------------------------------------------------------------------------------------------------
        // Attributes
        //----------------------------------------------------------------------------------------------------
        private string parameterPrefix = "{$$";
        private string parameterSuffix = "$$}";
        private string text;
        private Hashtable parameters;
        private bool htmlEncode = false;
        private SortedList<string, bool> overrideHtmlEncode = null;

        //----------------------------------------------------------------------------------------------------
        // Properties
        //----------------------------------------------------------------------------------------------------
        /// <summary>
        /// Gets or sets, if parameter and values should be html encoded
        /// </summary>
        public bool HtmlEncode {
            get {
                return htmlEncode;
            }
            set {
                htmlEncode = value;
            }
        }

        /// <summary>
        /// Parameter Prefix
        /// </summary>
        public string ParameterPrefix {
            get {
                return parameterPrefix;
            }
            set {
                parameterPrefix = value;
            }
        }

        /// <summary>
        /// Parameter Suffix
        /// </summary>
        public string ParameterSuffix {
            get {
                return parameterSuffix;
            }
            set {
                parameterSuffix = value;
            }
        }

        /// <summary>
        /// Main Text
        /// </summary>
        public string Text {
            get {
                return text;
            }
            set {
                text = value;
            }
        }

        /// <summary>
        /// Template Parameters
        /// </summary>
        public Hashtable Parameters {
            get {
                return parameters;
            }
        }

        //----------------------------------------------------------------------------------------------------
        // Constructor
        //----------------------------------------------------------------------------------------------------
        /// <summary>
        /// Creates a new instance
        /// </summary>
        public TemplateService() {
            Initialize();
        }

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="Text">Mail Template Body</param>
        public TemplateService(string Text) {
            text = Text;
            Initialize();
        }

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="Text">Mail Template Body</param>
        /// <param name="parameterPrefix">Parameter Prefix</param>
        /// <param name="parameterSuffix">Parameter Suffix</param>
        public TemplateService(string Text, string paramPrefix, string paramSuffix) {
            text = Text;
            this.parameterPrefix = paramPrefix;
            this.parameterSuffix = paramSuffix;
            Initialize();
        }

        //----------------------------------------------------------------------------------------------------
        // Methods
        //----------------------------------------------------------------------------------------------------
        /// <summary>
        /// Initializes the instance
        /// </summary>
        private void Initialize() {
            parameters = new Hashtable();
        }

        /// <summary>
        /// Adds a parameter with its value
        /// </summary>
        /// <param name="Parameter">Parameter without Prefix and suffix</param>
        /// <param name="Value">Parameter specific Value</param>
        public bool AddParameter(string Parameter, object Value) {
            if (Parameter != null) {
                if (Value == null) {
                    Value = string.Empty;
                }
                if (parameters.Contains(Parameter)) {
                    parameters.Remove(Parameter);
                }
                parameters.Add(Parameter, Value);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Add Parameter
        /// </summary>
        /// <param name="Parameter"></param>
        /// <param name="Value"></param>
        /// <param name="htmlEncode"></param>
        /// <returns></returns>
        public bool AddParameter(string Parameter, object Value, bool htmlEncode) {
            if (AddParameter(Parameter, Value)) {
                if (overrideHtmlEncode == null) {
                    overrideHtmlEncode = new SortedList<string, bool>();
                }
                overrideHtmlEncode.Add(Parameter, htmlEncode);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Get Overriden Html Encode
        /// </summary>
        /// <param name="Parameter"></param>
        /// <returns></returns>
        private bool GetOverridenHtmlEncode(string Parameter) {
            if (overrideHtmlEncode == null || overrideHtmlEncode.Count <= 0) {
                return htmlEncode;
            }

            if (!overrideHtmlEncode.ContainsKey(Parameter)) {
                return htmlEncode;
            }

            return overrideHtmlEncode[Parameter];
        }

        /// <summary>
        /// Clears all parameters
        /// </summary>
        public void ClearParameter() {
            parameters.Clear();
        }

        /// <summary>
        /// Starts the Template Processing
        /// </summary>
        /// <returns></returns>
        public void Process() {
            //------------------------------------------------------------------------------------------------------------------
            // Parameter - Value processing
            //------------------------------------------------------------------------------------------------------------------
            foreach (string Param in parameters.Keys) {
                string P = ParameterPrefix + Param + ParameterSuffix;
                string V = (parameters[Param] != null) ? parameters[Param].ToString() : string.Empty;

                if (htmlEncode && GetOverridenHtmlEncode(Param)) {
                    P = HttpUtility.HtmlEncode(P);
                    V = HttpUtility.HtmlEncode(V);
                }

                Text = Text.Replace(P, V);
            }

            //------------------------------------------------------------------------------------------------------------------
            // Special Characters
            //------------------------------------------------------------------------------------------------------------------
            //Text = ReplaceSpecialChars(Text);
        }

        /// <summary>
        /// Replaces special characters with html coded chars
        /// </summary>
        /// <param name="text"></param>
        private string ReplaceSpecialChars(string text) {
            if (string.IsNullOrEmpty(Text)) {
                return text;
            }
            text = text.Replace("Ü", "Uuml;");
            text = text.Replace("ü", "uuml;");
            text = text.Replace("Ö", "Ouml;");
            text = text.Replace("ö", "ouml;");
            text = text.Replace("Ä", "Auml;");
            text = text.Replace("ä", "auml;");
            text = text.Replace("ß", "&szlig;");
            return text;
        }

        /// <summary>
        /// Removes the place holders
        /// </summary>
        /// <param name="id"></param>
        /// <param name="element"></param>
        public void RemovePlaceHolder(string id, string element) {
            Regex regex = null;

            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(element) || string.IsNullOrEmpty(text)) {
                return;
            }

            regex = new Regex(string.Format("<{1}[^>]*?id=\"{0}\"[^>]*?>.*?</{1}>", id, element), RegexOptions.IgnoreCase | RegexOptions.Singleline);

            text = regex.Replace(text, string.Empty);
        }

        /// <summary>
        /// Clearn Up unused Placeholders and remove them
        /// </summary>
        public void CleanUpPlaceholders() {
            int s = text.IndexOf(parameterPrefix);
            if (s == -1) return;

            int e = text.IndexOf(parameterSuffix);
            if (e == -1) return;

            string value = text.Substring(s, e - s + parameterSuffix.Length);
            text = text.Replace(value, "");

            CleanUpPlaceholders();
        }
    }
}