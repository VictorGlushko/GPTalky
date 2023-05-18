using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core
{
    public static class GPTalkyHelper
    {
        static Dictionary<char,string> replacmentCharecters = new Dictionary<char,string>();

        static GPTalkyHelper()
        {
            //'_', '*', '[', ']', '(', ')', '~', '`', '>', 
            //'#', '+', '-', '=', '|', '{', '}', '.', '!'

            replacmentCharecters.Add('!', "\\!");
            replacmentCharecters.Add('.', "\\.");
            replacmentCharecters.Add('{', "\\}");
            replacmentCharecters.Add('}', "\\}");
            replacmentCharecters.Add('|', "\\|");
            replacmentCharecters.Add('=', "\\=");
            replacmentCharecters.Add('-', "\\-");
            replacmentCharecters.Add('+', "\\+");
            replacmentCharecters.Add('#', "\\#");
            replacmentCharecters.Add('>', "\\>");
            replacmentCharecters.Add('~', "\\~");
            replacmentCharecters.Add(')', "\\)");
            replacmentCharecters.Add('(', "\\(");
            replacmentCharecters.Add(']', "\\]");
            replacmentCharecters.Add('[', "\\[");
            replacmentCharecters.Add('*', "\\*");
            replacmentCharecters.Add('_', "\\_");
            //replacmentCharecters.Add('`', "\\`");
            replacmentCharecters.Add('\\', "\\\\");
        }



        public static string ReplaceSymbols(string input)
        {
            var inputSpan = input.AsSpan(); // Convert input string to a span of characters
            var outputSpan = new ReadOnlySpan<char>();

            int finalLen = inputSpan.Length;

            int i = 0;
            while (i != finalLen)
            {
                if (replacmentCharecters.ContainsKey(inputSpan[i]))
                {
                   var slicePartBefore = inputSpan.Slice(0, i);
                   var slicePartAfter = inputSpan.Slice(slicePartBefore.Length + 1, inputSpan.Length - slicePartBefore.Length - 1 );

                    var result = ConcatenateSpans(slicePartBefore, replacmentCharecters[inputSpan[i]]);
                    inputSpan = ConcatenateSpans(result, slicePartAfter);
                    finalLen = inputSpan.Length;
                    i++;
                }

                i++;
            }

            return new string(inputSpan); // Convert the span back to a string and return it
        }

        public static Span<char> ConcatenateSpans(ReadOnlySpan<char> span1, ReadOnlySpan<char> span2)
        {
            char[] result = new char[span1.Length + span2.Length]; // Create a new array with the combined length of the spans
            span1.CopyTo(result); // Copy the elements from span1 to the result array
            span2.CopyTo(result.AsSpan(span1.Length)); // Copy the elements from span2 to the result array starting from the end of span1

            return result.AsSpan(); // Convert the result array to a new span and return it
        }
    }
}
