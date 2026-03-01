using SharpGit2.Managed.Attributes;

namespace SharpGit2.Managed.Tests.Attributes;

public class AttributeFileTests
{
    [Fact]
    public void SimpleRead()
    {
        var filePath = Path.GetFullPath("Resources/Attributes/attr0");
        
        var file = GitAttributeFile.LoadStandalone(filePath);
        
        Assert.Equal(filePath, file.SourceInstance.FileName);
        Assert.Equal(1, file.Rules.Count);

        var rule = file.Rules[0];
        Assert.Equal("*", rule.Match.Pattern);
        Assert.NotEqual(default, rule.Match.Flags & GitAttributeFNMatchFlags.HasWild);
        Assert.Single(rule.Assigns);

        var assign = rule.Assigns.First();
        
        Assert.Equal("binary", assign.Key);
        Assert.True(ReferenceEquals(assign.Value, Constants.attribute_internal_true));
    }

    [Fact]
    public void MatchVariants()
    {
        var filePath = Path.GetFullPath("Resources/Attributes/attr1");
        
        var file = GitAttributeFile.LoadStandalone(filePath);
        
        Assert.Equal(filePath, file.SourceInstance.FileName);
        Assert.Equal(10, file.Rules.Count);
        
        GitAttributeRule rule;
        KeyValuePair<string, string> assign;

        rule = file.Rules[0];
        Assert.Equal("pat0", rule.Match.Pattern);
        Assert.Single(rule.Assigns);
        assign = rule.Assigns.First();
        Assert.Equal("attr0", assign.Key);
        Assert.True(ReferenceEquals(assign.Value, Constants.attribute_internal_true));

        rule = file.Rules[1];
        Assert.Equal("pat1", rule.Match.Pattern);
        Assert.NotEqual(default, rule.Match.Flags & GitAttributeFNMatchFlags.Negate);

        rule = file.Rules[2];
        Assert.Equal("pat2", rule.Match.Pattern);
        Assert.NotEqual(default, rule.Match.Flags & GitAttributeFNMatchFlags.Directory);
        
        rule = file.Rules[3];
        Assert.Equal("pat3dir/pat3file", rule.Match.Pattern);
        Assert.NotEqual(default, rule.Match.Flags & GitAttributeFNMatchFlags.FullPath);
        
        rule = file.Rules[4];
        Assert.Equal("pat4.*", rule.Match.Pattern);
        Assert.NotEqual(default, rule.Match.Flags & GitAttributeFNMatchFlags.HasWild);
        
        rule = file.Rules[5];
        Assert.Equal("*.pat5", rule.Match.Pattern);
        Assert.NotEqual(default, rule.Match.Flags & GitAttributeFNMatchFlags.HasWild);

        rule = file.Rules[7];
        Assert.Equal("pat7[a-e]??[xyz]", rule.Match.Pattern);
        Assert.Single(rule.Assigns);
        Assert.Equal(GitAttributeFNMatchFlags.HasWild, rule.Match.Flags);
        assign = rule.Assigns.First();
        Assert.Equal("attr7", assign.Key);
        Assert.True(ReferenceEquals(assign.Value, Constants.attribute_internal_true));
        
        rule = file.Rules[8];
        Assert.Equal("pat8 with spaces", rule.Match.Pattern);
        
        rule = file.Rules[9];
        Assert.Equal("pat9", rule.Match.Pattern);
    }

    [Fact]
    public void AssignVariants()
    {
        var file = GitAttributeFile.LoadStandalone(Path.GetFullPath("Resources/Attributes/attr2"));
        
        CheckOneAssignment(file, 0, "pat0", "simple", GitAttributeValue.True);
        CheckOneAssignment(file, 1, "pat1", "neg", GitAttributeValue.False);
        CheckOneAssignment(file, 2, "*", "notundef", GitAttributeValue.True);
        CheckOneAssignment(file, 3, "pat2", "notundef", GitAttributeValue.Unspecified);
        CheckOneAssignment(file, 4, "pat3", "assigned", "test-value");
        CheckOneAssignment(file, 5, "pat4", "rule-with-more-chars", "value-with-more-chars");
        CheckOneAssignment(file, 6, "pat5", "empty", GitAttributeValue.True);
        CheckOneAssignment(file, 7, "pat6", "negempty", GitAttributeValue.False);
        
        var rule = file.Rules[8];
        Assert.Equal("pat7", rule.Match.Pattern);
        Assert.Equal(5, rule.Assigns.Count);
        
        Assert.Contains("multiple", rule.Assigns.Keys);
        Assert.Equal(GitAttributeValue.True, rule.GetValueForAssign("multiple"));
        Assert.Contains("single", rule.Assigns.Keys);
        Assert.Equal(GitAttributeValue.False, rule.GetValueForAssign("single"));
        Assert.Contains("values", rule.Assigns.Keys);
        Assert.Equal((GitAttributeValue)"1", rule.GetValueForAssign("values"));
        Assert.Contains("also", rule.Assigns.Keys);
        Assert.Equal((GitAttributeValue)"a-really-long-value/*", rule.GetValueForAssign("also"));
        Assert.Contains("happy", rule.Assigns.Keys);
        Assert.Equal((GitAttributeValue)"yes!", rule.GetValueForAssign("happy"));
        Assert.DoesNotContain("other", rule.Assigns.Keys);

        rule = file.Rules[9];
        Assert.Equal("pat8", rule.Match.Pattern);
        Assert.Equal(2, rule.Assigns.Count);
        
        Assert.Contains("again", rule.Assigns.Keys);
        Assert.Equal(GitAttributeValue.True, rule.GetValueForAssign("again"));
        Assert.Contains("another", rule.Assigns.Keys);
        Assert.Equal((GitAttributeValue)"12321", rule.GetValueForAssign("another"));
        
        CheckOneAssignment(file, 10, "pat9", "at-eof", GitAttributeValue.False);

        static void CheckOneAssignment(
            GitAttributeFile file,
            int ruleIdx,
            string pattern,
            string name,
            GitAttributeValue expected)
        {
            var rule = file.Rules[ruleIdx];
            
            Assert.Equal(pattern, rule.Match.Pattern);
            Assert.Single(rule.Assigns);
            Assert.Contains(name, rule.Assigns.Keys);
            Assert.Equal(expected, rule.GetValueForAssign(name));
        }
    }

    private void AssertExamples(GitAttributeFile file)
    {
        Assert.Equal(3, file.Rules.Count);
        
        var rule = file.Rules[0];
        Assert.Equal("*.java", rule.Match.Pattern);
        Assert.Equal(3, rule.Assigns.Count);
        
        Assert.Contains("diff", rule.Assigns.Keys);
        Assert.Equal((GitAttributeValue)"java", rule.GetValueForAssign("diff"));
        Assert.Contains("crlf", rule.Assigns.Keys);
        Assert.Equal(GitAttributeValue.False, rule.GetValueForAssign("crlf"));
        Assert.Contains("myAttr", rule.Assigns.Keys);
        Assert.Equal(GitAttributeValue.True, rule.GetValueForAssign("myAttr"));
        Assert.DoesNotContain("missing", rule.Assigns.Keys);
        
        rule = file.Rules[1];
        Assert.Equal("NoMyAttr.java", rule.Match.Pattern);
        Assert.Single(rule.Assigns);
        Assert.Contains("myAttr", rule.Assigns.Keys);
        Assert.Equal(GitAttributeValue.Unspecified, rule.GetValueForAssign("myAttr"));

        rule = file.Rules[2];
        Assert.Equal("README", rule.Match.Pattern);
        Assert.Single(rule.Assigns);
        Assert.Contains("caveat", rule.Assigns.Keys);
        Assert.Equal((GitAttributeValue)"unspecified", rule.GetValueForAssign("caveat"));
    }

    [Fact]
    public void CheckAttributesExample()
    {
        var file = GitAttributeFile.LoadStandalone(Path.GetFullPath("Resources/Attributes/attr3"));
        
        AssertExamples(file);
    }
    
    [Fact]
    public void Whitespace()
    {
        var file = GitAttributeFile.LoadStandalone(Path.GetFullPath("Resources/Attributes/attr4"));
        
        AssertExamples(file);
    }
}