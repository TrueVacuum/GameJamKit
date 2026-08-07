using System.Collections.Generic;
using GameJamKit.Localization;
using NUnit.Framework;

namespace GameJamKit.Tests.Editor
{
    public sealed class LocalizationTests
    {
        [Test]
        public void CsvParser_HandlesQuotedCommasNewlinesAndQuotes()
        {
            const string csv =
                "key,en,zh-CN\n" +
                "greeting,Hello,你好\n" +
                "dialogue,\"Hello, traveler.\",\"第一行\n第二行\"\n" +
                "quote,\"She said \"\"Hi\"\".\",测试\n";

            List<string[]> rows = LocalizationCsvParser.Parse(csv);

            Assert.That(rows, Has.Count.EqualTo(4));
            Assert.That(rows[2][1], Is.EqualTo("Hello, traveler."));
            Assert.That(rows[2][2], Is.EqualTo("第一行\n第二行"));
            Assert.That(rows[3][1], Is.EqualTo("She said \"Hi\"."));
        }

        [Test]
        public void Database_MergesMultipleCsvSources()
        {
            LocalizationDatabase database = new LocalizationDatabase();
            database.MergeCsv("key,en,zh-CN\nmenu.play,Play,开始游戏\n", "UI");
            database.MergeCsv("key,en,zh-CN\ndialogue.001,Hello,你好\n", "Dialogue");

            Assert.That(database.TryGetText("en", "menu.play", out string play), Is.True);
            Assert.That(play, Is.EqualTo("Play"));
            Assert.That(database.TryGetText("zh-CN", "dialogue.001", out string dialogue), Is.True);
            Assert.That(dialogue, Is.EqualTo("你好"));
        }

        [Test]
        public void Database_DuplicateKeyUsesLaterValueAndReportsIssue()
        {
            LocalizationDatabase database = new LocalizationDatabase();
            List<string> issues = new List<string>();

            database.MergeCsv(
                "key,en\nmenu.play,Play\nmenu.play,Start\n",
                "UI",
                issues);

            Assert.That(database.TryGetText("en", "menu.play", out string text), Is.True);
            Assert.That(text, Is.EqualTo("Start"));
            Assert.That(issues, Has.Some.Contains("duplicated"));
        }

        [Test]
        public void Database_InvalidHeaderReportsIssue()
        {
            LocalizationDatabase database = new LocalizationDatabase();
            List<string> issues = new List<string>();

            database.MergeCsv("id,en\nmenu.play,Play\n", "Invalid", issues);

            Assert.That(issues, Has.Some.Contains("first header cell"));
            Assert.That(database.TryGetText("en", "menu.play", out _), Is.False);
        }

        [Test]
        public void CsvParser_UnterminatedQuoteThrows()
        {
            Assert.Throws<System.FormatException>(() =>
                LocalizationCsvParser.Parse("key,en\nmessage,\"unfinished"));
        }
    }
}
