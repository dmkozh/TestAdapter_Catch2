﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace Catch2Interface
{
    public class XmlOutput
    {
        #region Fields

        private StringBuilder _infobuilder = new StringBuilder();
        private StringBuilder _msgbuilder = new StringBuilder();
        private StringBuilder _stacktracebuilder = new StringBuilder();

        private int _testcasecount = 0;
        
        private Settings _settings;

        // Regex
        static readonly Regex _rgxTestCaseName = new Regex(@"^<TestCase name=""([^""]*)""");


        #endregion Fields

        #region Construction

        public XmlOutput(string xmloutput, bool timedout, Settings settings)
        {
            _settings = settings ?? new Settings();
            TimedOut = timedout;
            Xml = xmloutput;
            ProcessXml();
        }

        #endregion Costruction

        #region Properties
        
        public TimeSpan Duration { get; private set; }

        public bool IsPartialOutput { get; private set; } = false;

        public Reporter.OverallResults OverallResults { get; private set; }

        public List<TestResult> TestResults { get; private set; } = new List<TestResult>();

        public bool TimedOut { get; private set; } = false;

        public string Xml { get; private set; }

        #endregion Properties

        #region Public Static Metods

        public static bool IsVersion2Xml(string output)
        {
            return output.Contains(@"<Catch name=");
        }

        public static bool IsVersion3Xml(string output)
        {
            return output.Contains(@"<Catch2TestRun name=");
        }

        public static string CleanXml(string output)
        {
            if (IsVersion2Xml(output))
            {
                var idx = output.IndexOf(@"</Catch>"); // Find first occurance of </Catch>
                return idx == -1 ? string.Empty        // Make sure closing tag was found
                                 : output.Substring(0, idx+8);
                
            }
            else if(IsVersion3Xml(output))
            {
                var idx = output.IndexOf(@"</Catch2TestRun>"); // Find first occurance of </Catch2TestRun>
                return idx == -1 ? string.Empty                // Make sure closing tag was found
                                 : output.Substring(0, idx + 16);
            }

            return string.Empty;
        }

        #endregion Public Static Metods

        #region Public Methods

        public TestResult FindTestResult(string testcasename)
        {
            foreach( var result in TestResults)
            {
                if(result.Name == testcasename)
                {
                    return result;
                }
            }

            return null;
        }

        #endregion Public Methods

        #region Private Methods

        private void ExtractOverallResults(XmlNode nodeGroup)
        {
            var nodeOvRes = nodeGroup.SelectSingleNode("OverallResults");

            OverallResults = new Reporter.OverallResults(nodeOvRes);
        }

        private void ExtractTestResults(XmlNode nodeGroup)
        {
            // Retrieve data from TestCases that were run
            var nodesTestCases = nodeGroup.SelectNodes("TestCase");

            _testcasecount = nodesTestCases.Count;

            if (_testcasecount == 0)
            {
                // Special case. It appears there are no testcases.
                // We should tell the user about this
                return;
            }
            else
            {
                foreach (XmlNode nodeTestCase in nodesTestCases)
                {
                    ExtractTestCase(nodeTestCase);
                }
                ExtractOverallResults(nodeGroup);
            }
        }

        private void ExtractTestCase(XmlNode nodeTestCase)
        {
            var testcase = new Reporter.TestCase(nodeTestCase);

            // Create TestResult
            var result = new TestResult(testcase, _settings, true);

            TestResults.Add(result);
        }

        private void ProcessXml()
        {
            // Determine the part of the xmloutput string to parse
            // In some cases Catch2 output contains additional lines of output after the
            // xml-output. The XmlDocument parser doesn't like this so let's make sure those
            // extra lines are ignored.
            var cleanedoutput = XmlOutput.CleanXml(Xml);

            if (string.IsNullOrEmpty(cleanedoutput))
            {
                // Looks like we have a partial result.
                // Let's try to process as much as possible
                ProcessXmlPartial();
                return;
            }

            try
            {
                // Parse the Xml document
                var xml = new XmlDocument();
                xml.LoadXml(cleanedoutput);

                if (XmlOutput.IsVersion2Xml(cleanedoutput))
                {
                    var nodeGroup = xml.SelectSingleNode("Catch/Group");
                    ExtractTestResults(nodeGroup);
                    return;
                }
                else if (XmlOutput.IsVersion3Xml(cleanedoutput))
                {
                    var nodeGroup = xml.SelectSingleNode("Catch2TestRun");
                    ExtractTestResults(nodeGroup);
                    return;
                }
            }
            catch
            {
                // Someting went wrong parsing the XML
                // Treat as partial result and try to parse as much as possible
                TestResults.Clear(); // Cleanup any TestResults that may already have been processed
            }

            // Looks like we have a corrupted/partial result.
            // Let's try to process as much as possible
            ProcessXmlPartial();
        }

        private void ProcessXmlPartial()
        {
            IsPartialOutput = true;

            int idx_start = 0;
            int idx_end = 0;
            do
            {
                idx_start = Xml.IndexOf(@"<TestCase ", idx_end);
                if (idx_start == -1)
                {
                    break;
                }

                idx_end = Xml.IndexOf(@"</TestCase>", idx_start);
                if (idx_end == -1)
                {
                    ProcessPartialXmlTestCase(Xml.Substring(idx_start));
                }
                else
                {
                    ProcessXmlTestCase(Xml.Substring(idx_start, idx_end - idx_start + 11));
                }
            } while (idx_end >= 0);
        }

        private void ProcessXmlTestCase(string testcase)
        {
            try
            {
                var xml = new XmlDocument();
                xml.LoadXml(testcase);
                var nodeTestCase = xml.SelectSingleNode("TestCase");
                ExtractTestCase(nodeTestCase);
            }
            catch
            {
                // Someting went wrong parsing the XML
                // Ignore failure
            }
        }


        private void ProcessPartialXmlTestCase(string testcase)
        {
            // Do nothing in case timeout occured
            if (TimedOut) return;

            // Try to extract name testcase
            if (_rgxTestCaseName.IsMatch(testcase))
            {
                var mr = _rgxTestCaseName.Match(testcase);
                var name = mr.Groups[1].Value;
                var result = new TestResult(testcase, name, _settings, true, false);

                TestResults.Add(result);
            }
        }

        #endregion Private Methods
    }
}
