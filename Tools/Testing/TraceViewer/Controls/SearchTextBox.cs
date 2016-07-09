//-----------------------------------------------------------------------
// <copyright file="SearchTextBox.cs">
//      Copyright (c) Microsoft Corporation. All rights reserved.
// 
//      THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
//      EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//      MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
//      IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
//      CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//      TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
//      SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Microsoft.PSharp.Visualization
{
    /// <summary>
    /// Class implementing a search text box.
    /// </summary>
    public class SearchTextBox : TextBox
    {
        #region fields

        private DispatcherTimer SearchEventDelayTimer;

        #endregion

        #region properties

        public static DependencyProperty LabelTextProperty =
            DependencyProperty.Register(
                 "LabelText",
                 typeof(string),
                 typeof(SearchTextBox));

        public static DependencyProperty LabelTextColorProperty =
             DependencyProperty.Register(
                 "LabelTextColor",
                 typeof(Brush),
                 typeof(SearchTextBox));

        private static DependencyPropertyKey HasTextPropertyKey =
             DependencyProperty.RegisterReadOnly(
                 "HasText",
                 typeof(bool),
                 typeof(SearchTextBox),
                 new PropertyMetadata());
        public static DependencyProperty HasTextProperty =
            HasTextPropertyKey.DependencyProperty;

        private static DependencyPropertyKey IsMouseLeftButtonDownPropertyKey =
            DependencyProperty.RegisterReadOnly(
                "IsMouseLeftButtonDown",
                typeof(bool),
                typeof(SearchTextBox),
                new PropertyMetadata());
        public static DependencyProperty IsMouseLeftButtonDownProperty =
            IsMouseLeftButtonDownPropertyKey.DependencyProperty;

        public static DependencyProperty SearchEventTimeDelayProperty =
            DependencyProperty.Register(
                "SearchEventTimeDelay",
                typeof(Duration),
                typeof(SearchTextBox),
                new FrameworkPropertyMetadata(
                    new Duration(new TimeSpan(0, 0, 0, 0, 500)),
                    new PropertyChangedCallback(OnSearchEventTimeDelayChanged)));

        public static readonly RoutedEvent SearchEvent =
            EventManager.RegisterRoutedEvent(
                "Search",
                RoutingStrategy.Bubble,
                typeof(RoutedEventHandler),
                typeof(SearchTextBox));

        #endregion

        #region events

        public event RoutedEventHandler Search
        {
            add { AddHandler(SearchEvent, value); }
            remove { RemoveHandler(SearchEvent, value); }
        }

        #endregion

        #region constructors

        /// <summary>
        /// Static constructor.
        /// </summary>
        static SearchTextBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(SearchTextBox),
                new FrameworkPropertyMetadata(typeof(SearchTextBox)));
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public SearchTextBox() :
            base()
        {
            this.SearchEventDelayTimer = new DispatcherTimer();
            this.SearchEventDelayTimer.Interval = SearchEventTimeDelay.TimeSpan;
            this.SearchEventDelayTimer.Tick += new EventHandler(OnSeachEventDelayTimerTick);
        }

        #endregion

        #region actions

        protected override void OnTextChanged(TextChangedEventArgs e)
        {
            base.OnTextChanged(e);
            this.HasText = Text.Length != 0;
            this.SearchEventDelayTimer.Stop();
            this.SearchEventDelayTimer.Start();
        }

        private void IconBorder_MouseLeftButtonDown(object obj, MouseButtonEventArgs e)
        {
            this.IsMouseLeftButtonDown = true;
        }

        private void IconBorder_MouseLeftButtonUp(object obj, MouseButtonEventArgs e)
        {
            if (!this.IsMouseLeftButtonDown)
            {
                return;
            }

            this.Text = "";
            this.IsMouseLeftButtonDown = false;
        }

        private void IconBorder_MouseLeave(object obj, MouseEventArgs e)
        {
            this.IsMouseLeftButtonDown = false;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.Text = "";
            }
            else if (e.Key == Key.Return || e.Key == Key.Enter)
            {
                this.RaiseSearchEvent();
            }
            else
            {
                base.OnKeyDown(e);
            }
        }

        static void OnSearchEventTimeDelayChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            SearchTextBox stb = o as SearchTextBox;
            if (stb != null)
            {
                stb.SearchEventDelayTimer.Interval = ((Duration)e.NewValue).TimeSpan;
                stb.SearchEventDelayTimer.Stop();
            }
        }

        private void OnSeachEventDelayTimerTick(object o, EventArgs e)
        {
            this.SearchEventDelayTimer.Stop();
            this.RaiseSearchEvent();
        }

        #endregion

        #region methods

        public string LabelText
        {
            get { return (string)GetValue(LabelTextProperty); }
            set { SetValue(LabelTextProperty, value); }
        }

        public Brush LabelTextColor
        {
            get { return (Brush)GetValue(LabelTextColorProperty); }
            set { SetValue(LabelTextColorProperty, value); }
        }

        public bool HasText
        {
            get { return (bool)GetValue(HasTextProperty); }
            private set { SetValue(HasTextPropertyKey, value); }
        }

        public bool IsMouseLeftButtonDown
        {
            get { return (bool)GetValue(IsMouseLeftButtonDownProperty); }
            private set { SetValue(IsMouseLeftButtonDownPropertyKey, value); }
        }

        public Duration SearchEventTimeDelay
        {
            get { return (Duration)GetValue(SearchEventTimeDelayProperty); }
            set { SetValue(SearchEventTimeDelayProperty, value); }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            Border iconBorder = GetTemplateChild("PART_SearchIconBorder") as Border;
            if (iconBorder != null)
            {
                iconBorder.MouseLeftButtonDown += new MouseButtonEventHandler(IconBorder_MouseLeftButtonDown);
                iconBorder.MouseLeftButtonUp += new MouseButtonEventHandler(IconBorder_MouseLeftButtonUp);
                iconBorder.MouseLeave += new MouseEventHandler(IconBorder_MouseLeave);
            }
        }

        private void RaiseSearchEvent()
        {
            RoutedEventArgs args = new RoutedEventArgs(SearchEvent);
            base.RaiseEvent(args);
        }

        #endregion
    }
}
