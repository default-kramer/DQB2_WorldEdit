using Blocktavius.Core;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Blocktavius.AppDQB2
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private MainVM vm;

		public MainWindow()
		{
			InitializeComponent();

			vm = new MainVM();
			vm.Layers.Add(LayerVM.BuildChunkMask());
			vm.Layers.Add(new LayerVM());
			vm.SelectedLayer = vm.Layers.First();
			DataContext = vm;
		}

		private void PreviewButtonClicked(object sender, RoutedEventArgs e)
		{
			if (vm.Layers.Count < 2)
			{
				return;
			}

			var layer = vm.Layers[1];
			var painterData = layer.TileGridPainterVM;
			// TODO generate a hill from this layer and send it to playinful's app!
		}
	}
}