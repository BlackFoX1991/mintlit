using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace mintlit.backend
{
    public class keyPathCollection
    {

        const string regexDecimals = "^[+-]?[0-9]*$";
        const string regexfloat = @"^[0-9]*(?:\.[0-9]+)?$";
        const string regexBool = "^(?i:true|false)$";
        const string regexIdent = "[_a-zA-Z][_a-zA-Z0-9]{0,30}";
        public List<Keypath> Keys { get; set; }

        protected enum regex_types
        {
            STRING,
            DECIMAL,
            FLOAT,
            BOOLEAN
        }

        /// <summary>
        /// List of Tokens
        /// </summary>
        /// 
        protected TokenListe Tokens;

        /// <summary>
        /// Filename to load from
        /// </summary>
        public string? rawInput { get; set; }

        public int Count()
        {
            return Keys.Count();
        }
        public keyPathCollection(string mintfile)
        {
            Keys = new List<Keypath>();
            Tokens = new TokenListe();
            StreamReader gFile = new StreamReader(mintfile);
            rawInput = gFile.ReadToEnd();
            gFile.Close();
            tokenize();
            lexicalAnalyse();
        }
        public void addEndOfLine()
        {
            Keys.Add(new Keypath("$EOF", "", new List<keyValue>(), false));
        }
        public void addStartOfContainer()
        {
            Keys.Add(new Keypath("$SOC", "", new List<keyValue>(), false));
        }
        public void addEndOfContainer()
        {
            Keys.Add(new Keypath("$EOC", "", new List<keyValue>(), false));
        }
        /// <summary>
        /// Adds a new keypath
        /// </summary>
        /// <param name="key"></param>
        public void Add(Keypath key) { Keys.Add(key); }
        /// <summary>
        /// Returns if a Keypath exist/ not the Keyname
        /// </summary>
        /// <param name="keyPath"></param>
        /// <returns></returns>
        public bool ContainsKey(string keyPath)
        {
            bool cont = false;
            foreach (Keypath k in Keys) { if (k.fullpath == keyPath) { cont = true; break; } }
            return cont;
        }

        public int getIndexOf(string path, int offset = 0)
        {
            int index = -1;
            int count = 0;
            foreach (Keypath k in Keys)
            {

                if (k.fullpath == path)
                {
                    index = count + offset;
                    break;
                }
                count++;
            }
            return index;
        }
        public void deleteKey(string path)
        {
            if (path.Trim() == "") throw new Exception("The Path can not be empty.");
            if (!ContainsKey(path)) throw new Exception("The Path '" + path + "' does not exist.");
            int startIndex = getIndexOf(path);
            int endIndex = -1;
            int currentLevel = 0;


            for (int i = startIndex; i < Keys.Count; i++)
            {
                if (Keys[i].keyname == "$EOC")
                {
                    currentLevel--;
                    if (currentLevel == 0) { endIndex = i; break; }
                }
                if (Keys[i].keyname == "$SOC") currentLevel++;
            }
            Keys.RemoveRange(startIndex, ((endIndex + 1) - (startIndex + 1)));

        }

        public bool isKeyContainer(string path)
        {
            if (path.Trim() == "") throw new Exception("The Path can not be empty.");
            if (!ContainsKey(path)) throw new Exception("The Path '" + path + "' does not exist.");
            return Keys[getIndexOf(path)].isContainer;
        }

        private void setToContainer(string path)
        {
            int containerInsert = getIndexOf(path, 1);
            Keys.RemoveAt(containerInsert);
            containerInsert--;
            Keys.Insert(containerInsert, new Keypath("$SOC", "", new List<keyValue>(), false));
            containerInsert++;
            Keys.Insert(containerInsert, new Keypath("$EOC", "", new List<keyValue>(), false));
        }

        public List<string> getKeysInPath(string path, bool withCorePath = false, bool withContainers = true, int toLevel = 0)
        {
            if (path.Trim() != "")
            {
                if (!ContainsKey(path)) throw new Exception("The Path '" + path + "' does not exist.");
            }
            List<string> keyPaths = new List<string>();
            if (!Keys[getIndexOf(path)].isContainer)
            {
                if (withCorePath) keyPaths.Add(path);
            }
            int inLevel = 0;
            foreach (Keypath k in Keys)
            {
                if (k.keyname == "$EOF") continue;
                if (k.keyname == "$EOC") { inLevel--; continue; }
                if (k.keyname == "$SOC") { inLevel++; continue; }

                if (path.Trim() != "" && !withCorePath && k.fullpath == path) continue;
                if ((path.Trim() == "") || k.fullpath.StartsWith(path))
                {
                    if (!withContainers && k.isContainer) continue;
                    if (toLevel > 0)
                    {
                        string deepLevel = k.fullpath.Contains('/') ? k.fullpath.Substring(path.Length + 1) : k.fullpath.Substring(path.Length);
                        int level = deepLevel.Split('/').Count();
                        if (level <= toLevel) keyPaths.Add(k.fullpath);
                    }
                    else keyPaths.Add(k.fullpath);
                }
            }
            return keyPaths;
        }

        public void newKey(string path)
        {
            if (path.Trim() == "") throw new Exception("The Path can not be empty.");
            if (ContainsKey(path)) throw new Exception("The Path '" + path + "' does already exist.");
            if (path.Contains('/'))
            {
                if (!Keys[getIndexOf(path)].isContainer) setToContainer(path);
                Keys.Insert(getIndexOf(path, 2), new Keypath(path.Substring(path.LastIndexOf('/') + 1), path, new List<keyValue>(), false));
            }
            else
            {
                Keys.Add(new Keypath(path, path, new List<keyValue>(), false));
            }



        }

        public bool isKeyTable(string path)
        {

            if (path != "")
            {
                if (!ContainsKey(path)) throw new Exception("The Path '" + path + "' does not exist.");
            }
            List<string> gPaths = getKeysInPath(path, false, false, 1);
            int TableCount = -1;
            bool isTable = true;
            if (!isKeyContainer(path)) return false;
            foreach (string s in gPaths)
            {
                if (TableCount == -1) TableCount = Keys[getIndexOf(s)].kValues.Count();
                else
                {
                    if (Keys[getIndexOf(s)].kValues.Count() > TableCount || Keys[getIndexOf(s)].kValues.Count() < TableCount)
                    {
                        isTable = true;
                        break;
                    }
                }
            }
            return isTable;
        }
        public List<object> getKeyValuesByOrderIn(string path, int index = 0)
        {
            if (!isKeyTable(path))
            {
                throw new Exception("The Path is not a valid Table, make sure the Keys have the same amount of Values.");
            }
            if (path != "")
            {
                if (!ContainsKey(path)) throw new Exception("The Path '" + path + "' does not exist.");
            }
            List<object> values = new List<object>();
            List<string> paths = getKeysInPath(path, false, false, 1);
            foreach (string s in paths)
            {
                if (index > (Keys[getIndexOf(s)].kValues.Count() - 1)) throw new Exception("Out of Bounds in Path '" + s + "'.");
                values.Add(Keys[getIndexOf(s)].kValues[index].value);
            }
            return values;
        }
        public void setKeyValuesByOrderIn(string path, List<object> values, int index = 0)
        {
            if (!isKeyTable(path))
            {
                throw new Exception("The Path is not a valid Table, make sure the Keys have the same amount of Values.");
            }
            if (path != "")
            {
                if (!ContainsKey(path)) throw new Exception("The Path '" + path + "' does not exist.");
            }
            List<string> gPaths = getKeysInPath(path, false, false, 1);
            if (values.Count() < gPaths.Count || values.Count() > gPaths.Count) throw new Exception("Values need to match with the given single-Keys in the Path ('" + path + "')");
            int pos = 0;
            foreach (string s in gPaths)
            {
                if (index > (Keys[getIndexOf(s)].kValues.Count() - 1))
                {
                    keyValue K = new keyValue(s, values[pos]);
                    Keys[getIndexOf(s)].kValues.Add(K);
                }
                else Keys[getIndexOf(s)].kValues[index].value = values[pos];

                pos++;
            }
        }

        public void setKeyValue(string path, object value, int index = -1)
        {
            if (!ContainsKey(path)) throw new Exception("The Path '" + path + "' does not exist.");
            if ((Keys[getIndexOf(path)].kValues.Count() - 1) < index) throw new Exception("Out of Index in Path '" + path + "'");
            if (index == -1)
            {
                keyValue nKey = new keyValue(path, value);
                Keys[getIndexOf(path)].kValues.Add(nKey);
            }
            else Keys[getIndexOf(path)].kValues[index].value = value;
        }

        // setKeyName, getKeyName, getKeyValue, getKeyValues, newKey


        public string getRawData()
        {
            if (Keys.Count() == 0) return "";
            string tabsToInsert = "";
            string outputFile = "";
            foreach (Keypath K in Keys)
            {

                switch (K.keyname)
                {
                    case "$EOC":
                        tabsToInsert = tabsToInsert.Remove(0, "\t".Length);
                        outputFile += "\n" + tabsToInsert + "}\n";
                        continue;
                    case "$SOC":
                        outputFile += "\n" + tabsToInsert + "{";
                        tabsToInsert += "\t";
                        continue;
                    case "$EOF":
                        outputFile += ";\n";
                        continue;
                }
                outputFile += "!" + K.keyname;
                int c = 0;
                foreach (object V in K.kValues)
                {
                    if (K.kValues.Count == c + 1) outputFile += "\"" + V + "\"";
                    else outputFile += "\"" + V + "\",";
                    c++;
                }
            }
            return outputFile;
        }
        protected bool verifyExpect(List<token_typs.t_types> expList, token_typs.t_types curToken)
        {
            bool gotIt = false;
            foreach (token_typs.t_types T in expList)
            {
                if (curToken == T)
                {
                    gotIt = true;
                    break;
                }
            }
            return gotIt;
        }

        protected string expListToString(List<token_typs.t_types> expList)
        {
            string theList = "";
            foreach (token_typs.t_types T in expList)
            {
                theList += "\r\n" + T.ToString();
            }
            return theList;
        }
        protected void lexicalAnalyse(bool CustomTokens = false, TokenListe l_customTokens = null!)
        {

            List<token_typs.t_types> Next_Expected = new List<token_typs.t_types>();
            Next_Expected.Add(token_typs.t_types.setkey);
            Keypath toAdd = new Keypath();
            string currentPath = "";


            foreach (token_typs K in (!CustomTokens ? Tokens.getTokens() : l_customTokens.getTokens()))
            {

                if (!verifyExpect(Next_Expected, K.token_type)) throw new Exception("Expected following : " + expListToString(Next_Expected) + "\r\nbut found " + K.token_type.ToString() + "( Line : " + K.curLine + "/ Pos : " + K.curPos + " )");
                switch (K.token_type)
                {
                    case token_typs.t_types.setkey:
                        Next_Expected.Clear();
                        Next_Expected.Add(token_typs.t_types.ident);
                        toAdd = new Keypath();
                        break;

                    case token_typs.t_types.ident:


                        if (ContainsKey(K.token_value.ToString())) throw new Exception("Identifer '" + K.token_value + "' allready exist.");

                        Next_Expected.Clear();
                        Next_Expected.Add(token_typs.t_types.value);
                        Next_Expected.Add(token_typs.t_types.content_open);
                        Next_Expected.Add(token_typs.t_types.single_end);

                        currentPath += K.token_value;
                        toAdd.keyname = K.token_value.ToString()!;
                        toAdd.fullpath = currentPath;


                        break;

                    case token_typs.t_types.value:

                        Next_Expected.Clear();
                        Next_Expected.Add(token_typs.t_types.separator);
                        Next_Expected.Add(token_typs.t_types.content_open);
                        Next_Expected.Add(token_typs.t_types.single_end);
                        toAdd.kValues.Add(new keyValue(currentPath, K.token_value));

                        break;

                    case token_typs.t_types.separator:

                        Next_Expected.Clear();
                        Next_Expected.Add(token_typs.t_types.value);
                        break;

                    case token_typs.t_types.single_end:

                        Next_Expected.Clear();
                        Next_Expected.Add(token_typs.t_types.setkey);
                        Add(toAdd);
                        if (currentPath.Contains("/")) currentPath = currentPath.Substring(0, currentPath.LastIndexOf("/") + 1);
                        else currentPath = "";


                        if (currentPath.EndsWith("/")) Next_Expected.Add(token_typs.t_types.content_close);

                        addEndOfLine();

                        break;

                    case token_typs.t_types.content_open:

                        Next_Expected.Clear();
                        Next_Expected.Add(token_typs.t_types.setkey);
                        Next_Expected.Add(token_typs.t_types.content_close);
                        toAdd.isContainer = true;
                        Add(toAdd);
                        addStartOfContainer();
                        currentPath += "/";
                        break;


                    case token_typs.t_types.content_close:

                        Next_Expected.Clear();
                        Next_Expected.Add(token_typs.t_types.setkey);
                        addEndOfContainer();
                        if (currentPath.EndsWith("/")) currentPath = currentPath.Remove(currentPath.Length - 1, 1);

                        if (!currentPath.Contains("/")) currentPath = "";
                        else currentPath = currentPath.Substring(0, currentPath.LastIndexOf("/") + 1);

                        if (currentPath.EndsWith("/")) Next_Expected.Add(token_typs.t_types.content_close);
                        break;

                }
            }
            if (currentPath != "") throw new Exception("Path '" + (currentPath.EndsWith('/') ? currentPath.Substring(0, currentPath.LastIndexOf('/')) : currentPath) + "' at EOF need to be closed.");



        }

        public object convertValueToDatatype(string value)
        {
            regex_types get_type = regex_types.STRING;
            List<Regex> results = new List<Regex>
            {
                new Regex(regexDecimals),
                new Regex(regexfloat),
                new Regex(regexBool)
            };

            if (results[0].IsMatch(value)) get_type = regex_types.DECIMAL;
            else if (results[1].IsMatch(value)) get_type = regex_types.FLOAT;
            else if (results[2].IsMatch(value)) get_type = regex_types.BOOLEAN;
            else get_type = regex_types.STRING;

            switch (get_type)
            {
                case regex_types.STRING: return value.ToString();
                case regex_types.DECIMAL: return int.Parse(value, CultureInfo.InvariantCulture.NumberFormat);
                case regex_types.FLOAT: return float.Parse(value, CultureInfo.InvariantCulture.NumberFormat);
                case regex_types.BOOLEAN: return Convert.ToBoolean(value);
            }
            return value.ToString();
        }

        protected bool isIdentifer(string ident)
        {
            Regex Zustand = new Regex(regexIdent);
            return Zustand.IsMatch(ident);
        }
        /// <summary>
        /// Read Tokens from loaded File
        /// </summary>
        /// <exception cref="Exception"></exception>
        public void tokenize()
        {

            bool sReaderMode = false;
            string sValue = "";
            token_typs theToken = new token_typs();
            int curPos = 0;
            int curLine = 1;

            foreach (char S in rawInput!)
            {
                curPos++;
                if ((int)S == 10)
                {
                    curLine++;
                    curPos = 0;
                }
                if (S.ToString().Trim() == "" & !sReaderMode) continue;

                // Reading in String Mode till next Quote
                if (sReaderMode)
                {
                    if ((int)S == 34)
                    {
                        sReaderMode = false;
                        theToken = new token_typs();
                        // Get Datatypes
                        theToken.token_value = convertValueToDatatype(sValue);
                        theToken.token_type = token_typs.t_types.value;
                        theToken.curPos = curPos;
                        theToken.curLine = curLine;
                        Tokens.tAdd(theToken);
                        sValue = "";
                    }
                    else
                    {
                        sValue += S;
                    }

                }
                else
                {

                    /* Checking for Identifer till a special Char found.
                     * 
                     * Checks if the Identifer is valid
                     */
                    if (((int)S >= 65 && (int)S <= 90) ||
                        ((int)S >= 97 && (int)S <= 122) ||
                        S == '_')
                    {
                        sValue += S;
                    }
                    else
                    {
                        // checks if identifer value is empty 
                        if (sValue != "")
                        {
                            // check for identifer with RegEx
                            if (!isIdentifer(sValue))
                            {
                                throw new Exception("Invalid Identifer in Line " + curLine + " at Position " + curPos);
                            }
                            // Adding Identifer as Ident for Lexer
                            theToken = new token_typs();
                            theToken.token_value = sValue.ToString();
                            theToken.token_type = token_typs.t_types.ident;
                            theToken.curPos = curPos;
                            theToken.curLine = curLine;
                            Tokens.tAdd(theToken);
                            sValue = "";
                        }

                        // Checking for Operators
                        theToken = new token_typs();
                        theToken.curPos = curPos;
                        theToken.curLine = curLine;
                        switch (S)
                        {
                            case '!':
                                theToken.token_type = token_typs.t_types.setkey;
                                Tokens.tAdd(theToken);
                                break;
                            case '{':
                                theToken.token_type = token_typs.t_types.content_open;
                                Tokens.tAdd(theToken);
                                break;
                            case '}':
                                theToken.token_type = token_typs.t_types.content_close;
                                Tokens.tAdd(theToken);
                                break;
                            case ';':
                                theToken.token_type = token_typs.t_types.single_end;
                                Tokens.tAdd(theToken);
                                break;
                            case ',':
                                theToken.token_type = token_typs.t_types.separator;
                                Tokens.tAdd(theToken);
                                break;

                            case '"':
                                sReaderMode = true;
                                break;

                            default:
                                throw new Exception("Unknown Operator found at Line " + curLine + " on Position " + curPos);

                        }
                    }
                }
            }
        }

    }
}
