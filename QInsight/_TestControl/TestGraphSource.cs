using System.Collections.ObjectModel;
using Telerik.Windows.Diagrams.Core;

namespace Qenex.QInsight._TestControl;

public class TestGraphSource : IGraphSource 
{ 
    public TestGraphSource() 
    { 
        this.InternalItems = new ObservableCollection<TestNode>(); 
        this.InternalEdges = new ObservableCollection<TestEdge>(); 
    } 
 
    public ObservableCollection<TestNode> InternalItems 
    { 
        get; 
        private set; 
    } 
 
    public ObservableCollection<TestEdge> InternalEdges 
    { 
        get; 
        private set; 
    } 
 
    IEnumerable<ILink> IGraphSource.Links 
    { 
        get { return this.InternalEdges; } 
    } 
 
    System.Collections.IEnumerable IGraphSource.Items 
    { 
        get { return this.InternalItems; } 
    } 
} 