// UseWindowsForms + implicit usings pulls System.Windows.Forms into scope alongside WPF,
// making these type names ambiguous. The app is WPF; alias them to the WPF versions app-wide.
// (Drawing types like Point/Brush/Color are NOT aliased here — TrayIcon needs the System.Drawing
//  versions, so those stay file-scoped in the WPF files that need System.Windows.Media.)
global using UserControl = System.Windows.Controls.UserControl;
global using TextBox = System.Windows.Controls.TextBox;
global using MessageBox = System.Windows.MessageBox;
global using MessageBoxButton = System.Windows.MessageBoxButton;
global using MessageBoxResult = System.Windows.MessageBoxResult;
global using MessageBoxImage = System.Windows.MessageBoxImage;
global using KeyEventArgs = System.Windows.Input.KeyEventArgs;
global using MouseEventArgs = System.Windows.Input.MouseEventArgs;
global using Cursors = System.Windows.Input.Cursors;
