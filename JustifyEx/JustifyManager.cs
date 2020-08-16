using PdfSharp.Drawing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace JustifyEx
{
    public static class JustifyManager
    {
        // data - string to be justified
        // lineLength - Look at RDLC table properties, take first part of "size" parameter. for example 18.9 (in cm)
        // fontName - "Times New Roman", Arial, etc.
        // fontSize - 12, 14, etc.
        // 
        public static List<string> JustifyString(string data, double lineLength, string fontName, int fontSize, bool firstLineTablix)
        {
            //might be not accurate
            double convRate = 25;

            var pdfDoc = new PdfSharp.Pdf.PdfDocument();
            var pdfPage = pdfDoc.AddPage();
            var pdfGfx = PdfSharp.Drawing.XGraphics.FromPdfPage(pdfPage);

            double maxWidh = convRate * lineLength;
            //19 spaces, first line tablix
            string currentLine = firstLineTablix ? "" : "";
            double lengthWithNextWord = 0;
            var words = data.Split(' ');
            string tmpRes;
            List<string> dataList = new List<string>();
            bool firstIteration = true;

            bool textInBTag = false;

            for (int k = 0; k < words.Length; k++)
            {
                if (words[k].StartsWith("<b>") || words[k].StartsWith("<B>"))
                {
                    textInBTag = true;
                }

                tmpRes = words[k].Replace("<b>", "").Replace("<B>", "").Replace("</b>", "")
                    .Replace("</B>", "");

                if (textInBTag)
                {
                    tmpRes = "<b>" + tmpRes + "</b>";
                }

                if (words[k].EndsWith("</b>") || words[k].EndsWith("</B>"))
                {
                    textInBTag = false;
                }

                words[k] = tmpRes;
            }

            for (int i = 0; i < words.Length; i++)
            {
                //lengthWithNextWord = TextRenderer.MeasureText(currentLine + " " + words[i], new Font(fontName, fontSize)).Width;

                lengthWithNextWord = CalculateStringWidth(currentLine + " " + words[i], pdfGfx, fontName, fontSize, (firstLineTablix && firstIteration));

                if (lengthWithNextWord < maxWidh)
                {
                    currentLine = currentLine.Length == 0 ? words[i] : currentLine + " " + words[i];

                    if (i == words.Length - 1)
                        dataList.Add(currentLine);
                }

                if (lengthWithNextWord >= maxWidh)
                {
                    dataList.Add(currentLine);
                    currentLine = string.Empty;
                    currentLine = words[i];
                }
            }
            for (int j = 0; j < dataList.Count - 1; j++)
            {
                string strLine = dataList[j];
                dataList[j] = JustifyLine(strLine, maxWidh, pdfGfx, fontName, fontSize, (firstLineTablix && firstIteration));
                firstIteration = false;
            }

            return dataList;
        }

        private static string JustifyLine(string data, double? maxWidh, XGraphics grahpics, string fontName, int fontSize, bool needsTablix)
        {
            string newStr;
            double length;
            string toFind = String.Empty;
            string result = data;
            string toReplace = " ";
            bool NeedsAlignment = true;
            bool StringIsChanging = true;

            while (NeedsAlignment)
            {
                toFind += " ";
                toReplace += " ";
                StringIsChanging = true;

                while (StringIsChanging)
                {
                    newStr = ReplaceFirstOccurrence(result, toFind, toReplace);
                    if (newStr == result)
                        StringIsChanging = false;

                    length = CalculateStringWidth(newStr, grahpics, fontName, fontSize, needsTablix);
                    if (length < maxWidh)
                    {
                        result = newStr;
                    }
                    else
                    {
                        StringIsChanging = false;
                        NeedsAlignment = false;
                    }

                    newStr = ReplaceLastOccurrence(result, toFind, toReplace);
                    if (newStr == result)
                        StringIsChanging = false;

                    length = CalculateStringWidth(newStr, grahpics, fontName, fontSize, needsTablix);
                    if (length < maxWidh)
                    {
                        result = newStr;
                    }
                    else
                    {
                        StringIsChanging = false;
                        NeedsAlignment = false;
                    }
                }
            }

            return result;
        }

        private static double CalculateStringWidth(string line, XGraphics graphics, string fontName, int fontSize, bool needsTablix)
        {
            string takeAllInBRegex = @"(<b>.*?<\/b>)";

            double totalLength = 0;
            string boldedRes = string.Empty;
            string regularRes = string.Empty;

            var boldFont = new PdfSharp.Drawing.XFont(fontName, fontSize, XFontStyle.Bold);
            var regularFont = new PdfSharp.Drawing.XFont(fontName, fontSize, XFontStyle.Regular);

            var BoldeGroupdRes = Regex.Matches(line, takeAllInBRegex, RegexOptions.IgnoreCase);

            foreach (var boldgroup in BoldeGroupdRes)
            {
                string element = boldgroup.ToString().Replace("<b>", "").Replace("<B>", "").Replace("</b>", "")
                    .Replace("</B>", "");
                boldedRes += element;
            }

            regularRes = Regex.Replace(line, takeAllInBRegex, "", RegexOptions.IgnoreCase);

            int regularResSpacesCount = regularRes.ToCharArray().Count(c => c == ' ');
            string regularResSpaces = string.Empty;
            for (int i = 0; i < regularResSpacesCount; i++)
                regularResSpaces += " ";


            regularRes = regularRes.Replace(" ", "");

            double regularResSpacesLength = graphics.MeasureString(regularResSpaces, regularFont).Width;
            double regularLength = graphics.MeasureString(regularRes, regularFont).Width;
            double boldedLength = graphics.MeasureString(boldedRes, boldFont).Width;

            totalLength = regularLength + boldedLength + regularResSpacesLength + (needsTablix ? 60 : 0);

            return totalLength;
        }

        //jis iskart didina pirma, nes find ir replace jam gryzta 
        private static string ReplaceFirstOccurrence(string Source, string Find, string Replace)
        {
            string result = Source;
            var pattern = @"[^ ][ ]{" + Find.Length + "}[^ ]";

            var res = Regex.Matches(Source, pattern);

            if (res.Count > 0)
            {
                var strIndex = res[0].Index;
                result = Source.Remove(strIndex + 1, Find.Length).Insert(strIndex + 1, Replace);
            }
            return result;
        }

        private static string ReplaceLastOccurrence(string Source, string Find, string Replace)
        {
            string result = Source;
            var pattern = @"[^ ][ ]{" + Find.Length + "}[^ ]";

            var res = Regex.Matches(Source, pattern);

            if (res.Count > 0)
            {
                var strIndex = res[res.Count - 1].Index;
                result = Source.Remove(strIndex + 1, Find.Length).Insert(strIndex + 1, Replace);
            }
            return result;
        }
    }
}
