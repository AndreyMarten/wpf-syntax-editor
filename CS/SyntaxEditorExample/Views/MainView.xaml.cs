using SyntaxEditorExample.ViewModels;

namespace SyntaxEditorExample
{
    public partial class MainView
    {
        public MainView()
        {
            InitializeComponent();
            this.DataContext = new MainViewModel();
        }
    }
}
