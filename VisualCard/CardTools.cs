/*
 * MIT License
 *
 * Copyright (c) 2021-2022 EoflaOE and its companies
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 * 
 */

using System.Collections.Generic;
using System.IO;
using System.Text;
using VisualCard.Parsers;

namespace VisualCard
{
    /// <summary>
    /// Module for VCard management
    /// </summary>
    public static class CardTools
    {
        /// <summary>
        /// Gets the list of parsers for single/multiple contacts from the path
        /// </summary>
        /// <param name="Path">Path to the contacts file</param>
        /// <returns>List of contact parsers for single or multiple contacts</returns>
        public static List<BaseVcardParser> GetCardParsers(string Path)
        {
            // Variables and flags
            List<BaseVcardParser> FinalParsers = new();
            bool BeginSpotted = false;
            bool VersionSpotted = false;
            bool EndSpotted = false;

            // Open the stream to parse multiple contact versions (required to parse more than one contact)
            FileStream CardFs = new(Path, FileMode.Open, FileAccess.Read);
            StreamReader CardReader = new(CardFs);

            // Parse the lines of the card file
            string? CardLine;
            StringBuilder CardContent = new();
            string? CardVersion = "";
            CardLine = CardReader.ReadLine();
            while (!CardReader.EndOfStream)
            {
                // Get the line
                CardContent.AppendLine(CardLine);

                // If the line is empty, skip it
                if (!string.IsNullOrEmpty(CardLine))
                {
                    // All VCards must begin with BEGIN:VCARD
                    if (CardLine != "BEGIN:VCARD" && !BeginSpotted)
                        throw new InvalidDataException($"This file {Path} is not a valid VCard contact file.");
                    else if (!BeginSpotted)
                    {
                        BeginSpotted = true;
                        VersionSpotted = false;
                        EndSpotted = false;
                    }

                    // Now that the beginning of the card tag is spotted, parse the version as we need to know how to select the appropriate parser.
                    // All VCards are required to have their own version directly after the BEGIN:VCARD tag
                    CardLine = CardReader.ReadLine();
                    if (CardLine != "VERSION:2.1" && CardLine != "VERSION:3.0" && CardLine != "VERSION:4.0" && !VersionSpotted)
                        throw new InvalidDataException($"This file {Path} has an invalid VCard version {CardLine}.");
                    else if (!VersionSpotted)
                    {
                        VersionSpotted = true;
                        CardVersion = CardLine.Substring(8);
                    }

                    // If the ending tag is spotted, reset everything.
                    if (CardLine == "END:VCARD" && !EndSpotted)
                    {
                        EndSpotted = true;
                        CardContent.AppendLine(CardLine);

                        // Select parser
                        BaseVcardParser CardParser;
                        switch (CardVersion)
                        {
                            case "2.1":
                                CardParser = new VcardTwo(Path, CardContent.ToString(), CardVersion);
                                FinalParsers.Add(CardParser);
                                break;
                            case "3.0":
                                CardParser = new VcardThree(Path, CardContent.ToString(), CardVersion);
                                FinalParsers.Add(CardParser);
                                break;
                            case "4.0":
                                CardParser = new VcardFour(Path, CardContent.ToString(), CardVersion);
                                FinalParsers.Add(CardParser);
                                break;
                        }

                        // Clear the content in case we want to make a second contact
                        CardContent.Clear();
                        BeginSpotted = false;
                        CardLine = CardReader.ReadLine();
                    }
                }
                else
                    CardLine = CardReader.ReadLine();
            }

            // Throw if the card ended prematurely
            if (!EndSpotted)
                throw new InvalidDataException("Card ended prematurely without the ending tag");
            return FinalParsers;
        }
    }
}
