using SyntaxEditorExample.ViewModels;

namespace SyntaxEditorExample
{
    public partial class MainView
    {
        public MainView()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }
    }
}
