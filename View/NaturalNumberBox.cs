using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AigisCutter.View
{
    public class NaturalNumberBox : TextBox
    {
        public int Min
        {
            get { return (int)GetValue(MinProperty); }
            set { SetValue(MinProperty, value); }
        }

        public static readonly DependencyProperty MinProperty = DependencyProperty.Register(
            "Min",
            typeof(int),
            typeof(NaturalNumberBox),
            new PropertyMetadata(0));

        public int Max
        {
            get { return (int)GetValue(MaxProperty); }
            set { SetValue(MaxProperty, value); }
        }

        public static readonly DependencyProperty MaxProperty = DependencyProperty.Register(
            "Max",
            typeof(int),
            typeof(NaturalNumberBox),
            new PropertyMetadata(int.MaxValue));

        static NaturalNumberBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(NaturalNumberBox),
                new FrameworkPropertyMetadata(typeof(TextBox)));

            InputMethod.IsInputMethodEnabledProperty.OverrideMetadata(typeof(NaturalNumberBox), new FrameworkPropertyMetadata(false));
        }

        public NaturalNumberBox()
        {
            InputBindings.Add(new KeyBinding(ApplicationCommands.NotACommand, Key.V, ModifierKeys.Control));
            InputBindings.Add(new KeyBinding(ApplicationCommands.NotACommand, Key.Insert, ModifierKeys.Shift));
        }

        protected override void OnPreviewTextInput(TextCompositionEventArgs e)
        {
            int temp;
            if(!int.TryParse(e.Text, out temp))
                e.Handled = true;

            base.OnPreviewTextInput(e);
        }

        protected override void OnLostFocus(RoutedEventArgs e)
        {
            long num;
            if(!long.TryParse(Text, out num))
                Text = Min > 0 ? Min.ToString() : "0";

            if(num < Min)
                Text = Min.ToString();
            else if(num > Max)
                Text = Max.ToString();

            base.OnLostFocus(e);
        }
    }
}