using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mintlit.backend
{
    [Serializable]
    public class token_typs
    {
        public enum t_types
        {
            null_token,
            setkey,
            ident,
            value,
            separator,
            content_open,
            content_close,
            single_end
        }
        public t_types token_type;
        public object token_value;
        public int curPos;
        public int curLine;

        public token_typs()
        {
            token_type = t_types.null_token;
            token_value = new object();
            curPos = -1;
            curLine = -1;
        }
        public token_typs(t_types TokenType, string T_VALUE, int CurrentPos, int CurrentLine)
        {
            token_type = TokenType;
            token_value = T_VALUE;
            curPos = CurrentPos;
            curLine = CurrentLine;
        }

    }
    [Serializable]
    public class TokenListe
    {
        List<token_typs> ListOfTokens;
        public TokenListe() { ListOfTokens = new List<token_typs>(); }
        public void tAdd(token_typs token) { ListOfTokens.Add(token); }

        public List<token_typs> getTokens() { return ListOfTokens; }
    }
}
