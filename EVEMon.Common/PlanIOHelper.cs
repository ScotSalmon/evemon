﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using EVEMon.Common.Serialization.Exportation;
using EVEMon.Common.Serialization.Settings;
using EVEMon.Common.SettingsObjects;

namespace EVEMon.Common
{
    public delegate void ExportPlanEntryActions(StringBuilder builder, PlanEntry entry, PlanExportSettings settings);

    public static class PlanIOHelper
    {

        /// <summary>
        /// Exports the plan under a text format.
        /// </summary>
        /// <param name="planToExport"></param>
        /// <param name="settings"></param>
        /// <param name="exportActions"></param>
        /// <exception cref="NotImplementedException"></exception>
        /// <returns></returns>
        public static string ExportAsText(Plan planToExport, PlanExportSettings settings,
                                          ExportPlanEntryActions exportActions = null)
        {
            PlanScratchpad plan = new PlanScratchpad(planToExport.Character, planToExport);
            plan.Sort(planToExport.SortingPreferences);
            plan.UpdateStatistics();

            StringBuilder builder = new StringBuilder();
            const DescriptiveTextOptions TimeFormat =
                DescriptiveTextOptions.FullText | DescriptiveTextOptions.IncludeCommas | DescriptiveTextOptions.SpaceText;
            Character character = (Character)plan.Character;

            // Initialize constants
            string lineFeed = Environment.NewLine;
            string boldStart = String.Empty;
            string boldEnd = String.Empty;

            switch (settings.Markup)
            {
                case MarkupType.Forum:
                    boldStart = "[b]";
                    boldEnd = "[/b]";
                    break;
                case MarkupType.Html:
                    lineFeed = String.Format(CultureInfo.InvariantCulture, "<br />{0}", Environment.NewLine);
                    boldStart = "<b>";
                    boldEnd = "</b>";
                    break;
                case MarkupType.Undefined:
                case MarkupType.None:
                    break;
                default:
                    throw new NotImplementedException();
            }

            // Header
            if (settings.IncludeHeader)
            {
                builder.Append(boldStart);
                builder.AppendFormat(CultureConstants.DefaultCulture,
                                     settings.ShoppingList ? "Shopping list for {0}" : "Skill plan for {0}", character.Name);
                builder.Append(boldEnd);
                builder.Append(lineFeed);
                builder.Append(lineFeed);
            }

            // Scroll through entries
            int index = 0;
            DateTime endTime = DateTime.Now;
            foreach (PlanEntry entry in plan)
            {
                // Remapping point
                if (!settings.ShoppingList && entry.Remapping != null)
                {
                    builder.AppendFormat(CultureConstants.DefaultCulture, "***{0}***", entry.Remapping);
                    builder.Append(lineFeed);
                }

                // Skip is we're only build a shopping list
                bool shoppingListCandidate = !(entry.CharacterSkill.IsKnown || entry.Level != 1 || entry.CharacterSkill.IsOwned);
                if (settings.ShoppingList && !shoppingListCandidate)
                    continue;

                // Entry's index
                index++;
                if (settings.EntryNumber)
                    builder.AppendFormat(CultureConstants.DefaultCulture, "{0}. ", index);

                // Name
                builder.Append(boldStart);

                if (settings.Markup.Equals(MarkupType.Html))
                {
                    builder.AppendFormat(CultureConstants.DefaultCulture,
                                         !settings.ShoppingList
                                             ? "<a href=\"\" onclick=\"CCPEVE.showInfo({0})\">"
                                             : "<a href=\"\" onclick=\"CCPEVE.showMarketDetails({0})\">", entry.Skill.ID);
                }
                builder.Append(entry.Skill.Name);

                if (settings.Markup == MarkupType.Html)
                    builder.Append("</a>");

                if (!settings.ShoppingList)
                    builder.AppendFormat(CultureConstants.DefaultCulture, " {0}", Skill.GetRomanFromInt(entry.Level));

                builder.Append(boldEnd);

                // Training time
                if (settings.EntryTrainingTimes || settings.EntryStartDate || settings.EntryFinishDate ||
                    (settings.EntryCost && shoppingListCandidate))
                {
                    builder.Append(" (");
                    bool needComma = false;

                    // Training time
                    if (settings.EntryTrainingTimes)
                    {
                        needComma = true;
                        builder.Append(entry.TrainingTime.ToDescriptiveText(TimeFormat));
                    }

                    // Training start date
                    if (settings.EntryStartDate)
                    {
                        if (needComma)
                            builder.Append("; ");

                        needComma = true;

                        builder.AppendFormat(CultureConstants.DefaultCulture, "Start: {0}", entry.StartTime);
                    }

                    // Training end date
                    if (settings.EntryFinishDate)
                    {
                        if (needComma)
                            builder.Append("; ");

                        needComma = true;

                        builder.AppendFormat(CultureConstants.DefaultCulture, "Finish: {0}", entry.EndTime);
                    }

                    // Skill cost
                    if (settings.EntryCost && shoppingListCandidate)
                    {
                        if (needComma)
                            builder.Append("; ");

                        builder.AppendFormat(CultureConstants.DefaultCulture, "{0} ISK", entry.Skill.FormattedCost);
                    }

                    builder.Append(')');
                }

                if (exportActions != null)
                    exportActions(builder, entry, settings);

                builder.Append(lineFeed);

                // End time
                endTime = entry.EndTime;
            }

            // Footer
            if (settings.FooterCount || settings.FooterTotalTime || settings.FooterDate || settings.FooterCost)
            {
                builder.AppendLine(lineFeed);
                bool needComma = false;

                // Skills count
                if (settings.FooterCount)
                {
                    builder.AppendFormat(CultureConstants.DefaultCulture, "{0}{1}{2} unique skill{3}, ",
                                         boldStart, plan.GetUniqueSkillsCount(), boldEnd,
                                         (plan.GetUniqueSkillsCount() == 1 ? String.Empty : "s"));
                    builder.AppendFormat(CultureConstants.DefaultCulture, "{0}{1}{2} skill level{3}", boldStart, index, boldEnd,
                                         (index == 1 ? String.Empty : "s"));
                    needComma = true;
                }

                // Plan's training duration
                if (settings.FooterTotalTime)
                {
                    if (needComma)
                        builder.Append("; ");

                    needComma = true;

                    builder.AppendFormat(CultureConstants.DefaultCulture, "Total time: {0}{1}{2}",
                                         boldStart, plan.GetTotalTime(null, true).ToDescriptiveText(TimeFormat), boldEnd);
                }

                // End training date
                if (settings.FooterDate)
                {
                    if (needComma)
                        builder.Append("; ");

                    needComma = true;

                    builder.AppendFormat(CultureConstants.DefaultCulture, "Completion: {0}{1}{2}", boldStart, endTime, boldEnd);
                }

                // Total books cost
                if (settings.FooterCost)
                {
                    if (needComma)
                        builder.Append("; ");

                    string formattedIsk = String.Format(CultureConstants.TidyInteger, "{0:n}", plan.NotKnownSkillBooksCost);
                    builder.AppendFormat(CultureConstants.DefaultCulture, "Cost: {0}{1}{2}", boldStart, formattedIsk, boldEnd);
                }

                // Warning about skill costs
                builder.Append(lineFeed);
                if (settings.FooterCost || settings.EntryCost)
                    builder.Append("N.B. Skill costs are based on CCP's database and are indicative only");
            }

            // Returns the text representation.
            return builder.ToString();
        }

        /// <summary>
        /// Exports the plan under an XML format.
        /// </summary>
        /// <param name="plan">The plan.</param>
        /// <returns></returns>
        public static string ExportAsXML(Plan plan)
        {
            // Generates a settings plan and transforms it to an output plan
            SerializablePlan serial = plan.Export();
            OutputPlan output = new OutputPlan { Name = serial.Name, Owner = serial.Owner, Revision = Settings.Revision };
            output.Entries.AddRange(serial.Entries);

            // Serializes to XML document and gets a string representation
            XmlDocument doc = Util.SerializeToXmlDocument(typeof(OutputPlan), output);
            return Util.GetXMLStringRepresentation(doc);
        }

        /// <summary>
        /// Exports the plan under an XML format.
        /// </summary>
        /// <param name="plans">The plans.</param>
        /// <returns></returns>
        public static string ExportAsXML(IEnumerable<Plan> plans)
        {
            OutputPlans output = new OutputPlans { Revision = Settings.Revision };
            output.Plans.AddRange(plans.Select(plan => plan.Export()));

            // Serializes to XML document and gets a string representation
            XmlDocument doc = Util.SerializeToXmlDocument(typeof(OutputPlans), output);
            return Util.GetXMLStringRepresentation(doc);
        }

        /// <summary>
        /// Imports a <see cref="SerializablePlan"/> from the given filename. Works with old and new formats.
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static SerializablePlan ImportFromXML(String filename)
        {
            SerializablePlan result = null;
            try
            {
                // Is the format compressed ? 
                if (filename.EndsWith(".emp"))
                {
                    string tempFile = Util.UncompressToTempFile(filename);
                    try
                    {
                        return ImportFromXML(tempFile);
                    }
                    finally
                    {
                        File.Delete(tempFile);
                    }
                }

                // Reads the revision number from the file
                int revision = Util.GetRevisionNumber(filename);

                // Old format
                result = revision == 0
                             ? Util.DeserializeXML<SerializablePlan>(filename,
                                                                     Util.LoadXSLT(Properties.Resources.SettingsAndPlanImport))
                             : Util.DeserializeXML<OutputPlan>(filename);
            }
            catch (UnauthorizedAccessException exc)
            {
                MessageBox.Show("Couldn't read the given file, access was denied. Maybe the directory was under synchronization.");
                ExceptionHandler.LogException(exc, true);
            }
            catch (InvalidDataException exc)
            {
                MessageBox.Show("The file seems to be corrupted, wrong gzip format.");
                ExceptionHandler.LogException(exc, true);
            }

            if (result == null)
                MessageBox.Show("There was a problem with the format of the document.");

            return result;
        }

        /// <summary>
        /// Imports plans from the given filename.
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <returns></returns>
        public static IEnumerable<SerializablePlan> ImportPlansFromXML(String filename)
        {
            OutputPlans result = null;
            try
            {
                // Is the format compressed ? 
                if (filename.EndsWith(".epb"))
                {
                    string tempFile = Util.UncompressToTempFile(filename);
                    try
                    {
                        return ImportPlansFromXML(tempFile);
                    }
                    finally
                    {
                        File.Delete(tempFile);
                    }
                }

                // Reads the revision number from the file
                int revision = Util.GetRevisionNumber(filename);

                if (revision != 0)
                    result = Util.DeserializeXML<OutputPlans>(filename);
            }
            catch (UnauthorizedAccessException exc)
            {
                MessageBox.Show("Couldn't read the given file, access was denied. Maybe the directory was under synchronization.");
                ExceptionHandler.LogException(exc, true);
            }
            catch (InvalidDataException exc)
            {
                MessageBox.Show("The file seems to be corrupted, wrong gzip format.");
                ExceptionHandler.LogException(exc, true);
            }

            if (result == null)
            {
                MessageBox.Show("There was a problem with the format of the document.");
                return null;
            }

            return result.Plans;
        }
    }
}