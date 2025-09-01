using Telerik.Windows.Diagrams.Core;

namespace Qenex.QInsight._TestControl;

public class TestEdge : ILink<TestNode> 
{ 
    public TestNode Source 
    { 
        get; 
        set; 
    } 
 
    public TestNode Target 
    { 
        get; 
        set; 
    } 
 
    object ILink.Source 
    { 
        get 
        { 
            return this.Source; 
        } 
        set 
        { 
        } 
 
    } 
 
    object ILink.Target 
    { 
        get 
        { 
            return this.Target; 
        } 
        set 
        { 
        } 
    } 
} 