using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrackerConsole
{
  // Based on https://stackoverflow.com/questions/3961278/word-wrap-a-string-in-multiple-lines
  public static class WordWrap
  {
    public static List<string> Wrap(this string the_string, int width)
    {
      int pos, next;

      // Lucidity check
      if (width < 1)
        return new List<string> { the_string };

      if (string.IsNullOrWhiteSpace(the_string))
        return new List<string>();

      List<string> list = new List<string>();


      // Parse each line of text
      for (pos = 0; pos < the_string.Length; pos = next)
      {
        // Find end of line
        int eol = the_string.IndexOf("\n", pos);

        if (eol == -1)
          next = eol = the_string.Length;
        else
          next = eol + "\n".Length;

        // Copy this line of text, breaking into smaller lines as needed
        if (eol > pos)
        {
          do
          {
            int len = eol - pos;

            if (len > width)
              len = BreakLine(the_string, pos, width);

            list.Add(the_string.Substring(pos, len));

            // Trim whitespace following break
            pos += len;

            while (pos < eol && Char.IsWhiteSpace(the_string[pos]))
              pos++;

          } while (eol > pos);
        }
        else list.Add(string.Empty); // Empty line
      }

      return list;
    }

    /// <summary>
    /// Locates position to break the given line so as to avoid
    /// breaking words.
    /// </summary>
    /// <param name="text">String that contains line of text</param>
    /// <param name="pos">Index where line of text starts</param>
    /// <param name="max">Maximum line length</param>
    /// <returns>The modified line length</returns>
    public static int BreakLine(string text, int pos, int max)
    {
      // Find last whitespace in line
      int i = max - 1;
      while (i >= 0 && !Char.IsWhiteSpace(text[pos + i]))
        i--;
      if (i < 0)
        return max; // No whitespace found; break at maximum length
                    // Find start of whitespace
      while (i >= 0 && Char.IsWhiteSpace(text[pos + i]))
        i--;
      // Return length of text before whitespace
      return i + 1;
    }
  }

}