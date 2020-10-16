using Atomus.Diagnostics;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Atomus.Control.ErrorAlert
{
    public class DefaultErrorAlert : IErrorAlert
    {
        private readonly ErrorProvider errorProvider;
        private readonly Dictionary<System.Windows.Forms.Control, Padding> controlsOrgPadding;
        private readonly Dictionary<System.Windows.Forms.Control, Size> controlsOrgSize;
        private static Icon icon;
        
        public DefaultErrorAlert()
        {
            this.errorProvider = new ErrorProvider();

            this.controlsOrgPadding = new Dictionary<System.Windows.Forms.Control, Padding>();
            this.controlsOrgSize = new Dictionary<System.Windows.Forms.Control, Size>();
            
            if (icon == null)
                this.GetIcon();
            else
                this.errorProvider.Icon = icon;

            try
            {
                ((IErrorAlert)this).BlinkStyle = (ErrorBlinkStyle)Enum.Parse(typeof(ErrorBlinkStyle), this.GetAttribute("BlinkStyle"));
            }
            catch (Exception exception)
            {
                DiagnosticsTool.MyTrace(exception);
            }
        }

        private async void GetIcon()
        {
            try
            {
                icon = await this.GetAttributeWebIcon("Icon");

                if (icon != null)
                    this.errorProvider.Icon = icon;
            }
            catch (Exception exception)
            {
                DiagnosticsTool.MyTrace(exception);
            }
        }

        public DefaultErrorAlert(ContainerControl _ContainerControl) : this()
        {
            this.errorProvider = new ErrorProvider(_ContainerControl);
        }

        bool IErrorAlert.Result { get; set; }// = true;

        int IErrorAlert.BlinkRate
        {
            get
            {
                return this.errorProvider.BlinkRate;
            }
            set
            {
                this.errorProvider.BlinkRate = value;
            }
        }
        ErrorBlinkStyle IErrorAlert.BlinkStyle
        {
            get
            {
                return this.errorProvider.BlinkStyle;
            }
            set
            {
                this.errorProvider.BlinkStyle = value;
            }
        }
        Icon IErrorAlert.Icon
        {
            get
            {
                return this.errorProvider.Icon;
            }
            set
            {
                this.errorProvider.Icon = value;
            }
        }
        ErrorIconAlignment IErrorAlert.IconAlignment { get; set; } = ErrorIconAlignment.MiddleRight;
        int IErrorAlert.IconPadding { get; set; } = 0;

        void IErrorAlert.ControlIconAlignment(System.Windows.Forms.Control control)
        {
            ((IErrorAlert)this).ControlIconAlignment(control, ErrorIconAlignment.MiddleRight);
        }
        void IErrorAlert.ControlIconAlignment(System.Windows.Forms.Control control, ErrorIconAlignment errorIconAlignment)
        {
            this.errorProvider.SetIconAlignment(control, errorIconAlignment);
        }

        void IErrorAlert.ControlIconPadding(System.Windows.Forms.Control control)
        {
            ((IErrorAlert)this).ControlIconPadding(control, 0);
        }
        void IErrorAlert.ControlIconPadding(System.Windows.Forms.Control control, int iconPadding)
        {
            this.errorProvider.SetIconPadding(control, iconPadding);
        }

        bool IErrorAlert.Checked(params System.Windows.Forms.Control[] checkBoxs)
        {
            return ((IErrorAlert)this).Checked(" ", checkBoxs);
        }
        bool IErrorAlert.Checked(string message, params System.Windows.Forms.Control[] checkBoxs)
        {
            bool result;

            result = true;

            foreach (System.Windows.Forms.Control checkBox in checkBoxs)
            {
                if (!Checked(message, checkBox))
                    result = false;
            }

            return result;
        }
        bool Checked(string message, System.Windows.Forms.Control checkBox)
        {
            bool result;

            if (checkBox is CheckBox)
                result = ((CheckBox)checkBox).Checked;
            else
                throw new AtomusException("Not Support Control.");

            if (!result)
                ((IErrorAlert)this).SetError(checkBox, message.Translate());

            return result;
        }

        bool IErrorAlert.UnChecked(params System.Windows.Forms.Control[] checkBoxs)
        {
            return ((IErrorAlert)this).UnChecked(" ", checkBoxs);
        }
        bool IErrorAlert.UnChecked(string message, params System.Windows.Forms.Control[] checkBoxs)
        {
            bool result;

            result = true;
            foreach (System.Windows.Forms.Control checkBox in checkBoxs)
            {
                if (!UnChecked(message, checkBox))
                    result = false;
            }

            return result;
        }
        bool UnChecked(string message, System.Windows.Forms.Control checkBox)
        {
            bool result;

            if (checkBox is CheckBox)
                result = !((CheckBox)checkBox).Checked;
            else
                throw new AtomusException("Not Support Control.");

            if (!result)
                ((IErrorAlert)this).SetError(checkBox, message.Translate());

            return result;
        }

        void IErrorAlert.Clear()
        {
            foreach (System.Windows.Forms.Control control in this.controlsOrgPadding.Keys)
            {
                control.Margin = this.controlsOrgPadding[control];
                control.Size = this.controlsOrgSize[control];
                control.TextChanged -= this.TextChanged;
            }

            this.controlsOrgPadding.Clear();
            this.errorProvider.Clear();
            ((IErrorAlert)this).Result = true;
        }
        void IErrorAlert.Clear(System.Windows.Forms.Control control)
        {
            if (this.controlsOrgPadding.ContainsKey(control))
            {
                control.Margin = this.controlsOrgPadding[control];
                control.Size = this.controlsOrgSize[control];
                control.TextChanged -= this.TextChanged;

                this.controlsOrgPadding.Remove(control);

                this.errorProvider.SetError(control, "");

                if (this.controlsOrgPadding.Count == 0)
                    ((IErrorAlert)this).Result = true;
            }
        }

        void IErrorAlert.SetError(System.Windows.Forms.Control control)
        {
            ((IErrorAlert)this).SetError(control, " ");
        }
        void IErrorAlert.SetError(System.Windows.Forms.Control control, string message)
        {
            if (!this.controlsOrgPadding.ContainsKey(control))
                this.controlsOrgPadding.Add(control, control.Margin);

            if (!this.controlsOrgSize.ContainsKey(control))
                this.controlsOrgSize.Add(control, control.Size);

            control.DoubleBuffered(true);

            if (control.Dock == DockStyle.Fill)
                control.Margin = new Padding(control.Margin.Left, control.Margin.Top, control.Margin.Right + this.errorProvider.Icon.Width, control.Margin.Bottom);
            else
                control.Size = new Size(control.Width - this.errorProvider.Icon.Width, control.Height);

            if (message.Equals(""))
                this.errorProvider.SetError(control, " ");
            else
                this.errorProvider.SetError(control, message.Translate());

            if (((IErrorAlert)this).Result)
                ((IErrorAlert)this).Result = false;

            if (control is CheckBox)
                ((CheckBox)control).CheckedChanged += TextChanged;
            else if (control is NumericUpDown)
                ((NumericUpDown)control).ValueChanged += TextChanged;
            else
                control.TextChanged += TextChanged;
        }

        void TextChanged(Object sender, EventArgs e)
        {
            ((IErrorAlert)this).Clear((System.Windows.Forms.Control)sender);
        }

        bool IErrorAlert.TextContains(string value, params System.Windows.Forms.Control[] controls)
        {
            return ((IErrorAlert)this).TextContains(value, " ", controls);
        }
        bool IErrorAlert.TextContains(string value, string message, params System.Windows.Forms.Control[] controls)
        {
            bool result;

            result = true;
            foreach (System.Windows.Forms.Control control in controls)
            {
                if (!TextContains(value, message, control))
                    result = false;
            }

            return result;
        }
        bool TextContains(string value, string message, System.Windows.Forms.Control control)
        {
            bool _Result;

            _Result = control.Text.Contains(value);

            if (!_Result)
                ((IErrorAlert)this).SetError(control, message.Translate());

            return _Result;
        }

        bool IErrorAlert.TextNotContains(string value, params System.Windows.Forms.Control[] controls)
        {
            return ((IErrorAlert)this).TextNotContains(value, " ", controls);
        }
        bool IErrorAlert.TextNotContains(string value, string message, params System.Windows.Forms.Control[] controls)
        {
            bool result;

            result = true;
            foreach (System.Windows.Forms.Control control in controls)
            {
                if (!TextNotContains(value, message, control))
                    result = false;
            }

            return result;
        }
        bool TextNotContains(string value, string message, System.Windows.Forms.Control control)
        {
            bool result;

            result = !control.Text.Contains(value);

            if (!result)
                ((IErrorAlert)this).SetError(control, message.Translate());

            return result;
        }

        bool IErrorAlert.TextEndsWith(string value, params System.Windows.Forms.Control[] controls)
        {
            return ((IErrorAlert)this).TextEndsWith(value, " ", controls);
        }
        bool IErrorAlert.TextEndsWith(string value, string message, params System.Windows.Forms.Control[] controls)
        {
            bool result;

            result = true;
            foreach (System.Windows.Forms.Control control in controls)
            {
                if (!TextEndsWith(value, message, control))
                    result = false;
            }

            return result;
        }
        bool TextEndsWith(string value, string message, System.Windows.Forms.Control control)
        {
            bool result;

            result = control.Text.EndsWith(value);

            if (!result)
                ((IErrorAlert)this).SetError(control, message.Translate());

            return result;
        }

        bool IErrorAlert.TextEqual(string value, params System.Windows.Forms.Control[] controls)
        {
            return ((IErrorAlert)this).TextEqual(value, " ", controls);
        }
        bool IErrorAlert.TextEqual(string value, string message, params System.Windows.Forms.Control[] controls)
        {
            bool result;

            result = true;
            foreach (System.Windows.Forms.Control control in controls)
            {
                if (!TextEqual(value, message, control))
                    result = false;
            }

            return result;
        }
        bool TextEqual(string value, string message, System.Windows.Forms.Control control)
        {
            bool result;

            result = control.Text.Equals(value);

            if (!result)
                ((IErrorAlert)this).SetError(control, message.Translate());

            return result;
        }

        bool IErrorAlert.TextNotEqual(string value, params System.Windows.Forms.Control[] controls)
        {
            return ((IErrorAlert)this).TextNotEqual(value, " ", controls);
        }
        bool IErrorAlert.TextNotEqual(string value, string message, params System.Windows.Forms.Control[] controls)
        {
            bool result;

            result = true;
            foreach (System.Windows.Forms.Control control in controls)
            {
                if (!TextNotEqual(value, message, control))
                    result = false;
            }

            return result;
        }
        bool TextNotEqual(string value, string message, System.Windows.Forms.Control control)
        {
            bool result;

            result = !control.Text.Equals(value);

            if (!result)
                ((IErrorAlert)this).SetError(control, message.Translate());

            return result;
        }

        bool IErrorAlert.TextLengthEqual(int length, params System.Windows.Forms.Control[] controls)
        {
            return ((IErrorAlert)this).TextLengthEqual(length, " ", controls);
        }
        bool IErrorAlert.TextLengthEqual(int length, string message, params System.Windows.Forms.Control[] controls)
        {
            bool result;

            result = true;
            foreach (System.Windows.Forms.Control control in controls)
            {
                if (!TextLengthEqual(length, message, control))
                    result = false;
            }

            return result;
        }
        bool TextLengthEqual(int length, string message, System.Windows.Forms.Control control)
        {
            bool result;

            result = (control.Text.Length == length);

            if (!result)
                ((IErrorAlert)this).SetError(control, message.Translate());

            return result;
        }



        bool IErrorAlert.TextLengthGreaterThan(int length, params System.Windows.Forms.Control[] controls)
        {
            return ((IErrorAlert)this).TextLengthGreaterThan(length, " ", controls);
        }
        bool IErrorAlert.TextLengthGreaterThan(int length, string message, params System.Windows.Forms.Control[] controls)
        {
            bool result;

            result = true;
            foreach (System.Windows.Forms.Control control in controls)
            {
                if (!TextLengthGreaterThan(length, message, control))
                    result = false;
            }

            return result;
        }
        bool TextLengthGreaterThan(int length, string message, System.Windows.Forms.Control control)
        {
            bool result;

            result = (control.Text.Length > length);

            if (!result)
                ((IErrorAlert)this).SetError(control, message.Translate());

            return result;
        }

        bool IErrorAlert.TextLengthGreaterThanOrEqual(int length, params System.Windows.Forms.Control[] controls)
        {
            return ((IErrorAlert)this).TextLengthGreaterThanOrEqual(length, " ", controls);
        }
        bool IErrorAlert.TextLengthGreaterThanOrEqual(int length, string message, params System.Windows.Forms.Control[] controls)
        {
            bool result;

            result = true;
            foreach (System.Windows.Forms.Control control in controls)
            {
                if (!TextLengthGreaterThanOrEqual(length, message, control))
                    result = false;
            }

            return result;
        }
        bool TextLengthGreaterThanOrEqual(int length, string message, System.Windows.Forms.Control control)
        {
            bool result;

            result = (control.Text.Length >= length);

            if (!result)
                ((IErrorAlert)this).SetError(control, message.Translate());

            return result;
        }

        bool IErrorAlert.TextLengthLessThan(int length, params System.Windows.Forms.Control[] controls)
        {
            return ((IErrorAlert)this).TextLengthLessThan(length, " ", controls);
        }
        bool IErrorAlert.TextLengthLessThan(int length, string message, params System.Windows.Forms.Control[] controls)
        {
            bool result;

            result = true;
            foreach (System.Windows.Forms.Control control in controls)
            {
                if (!TextLengthLessThan(length, message, control))
                    result = false;
            }

            return result;
        }
        bool TextLengthLessThan(int length, string message, System.Windows.Forms.Control control)
        {
            bool result;

            result = (control.Text.Length < length);

            if (!result)
                ((IErrorAlert)this).SetError(control, message.Translate());

            return result;
        }

        bool IErrorAlert.TextLengthLessThanOrEqual(int length, params System.Windows.Forms.Control[] controls)
        {
            return ((IErrorAlert)this).TextLengthLessThanOrEqual(length, " ", controls);
        }
        bool IErrorAlert.TextLengthLessThanOrEqual(int length, string message, params System.Windows.Forms.Control[] controls)
        {
            bool result;

            result = true;
            foreach (System.Windows.Forms.Control control in controls)
            {
                if (!TextLengthLessThanOrEqual(length, message, control))
                    result = false;
            }

            return result;
        }
        bool TextLengthLessThanOrEqual(int length, string message, System.Windows.Forms.Control control)
        {
            bool result;

            result = (control.Text.Length <= length);

            if (!result)
                ((IErrorAlert)this).SetError(control, message.Translate());

            return result;
        }

        bool IErrorAlert.TextStartsWith(string value, params System.Windows.Forms.Control[] controls)
        {
            return ((IErrorAlert)this).TextStartsWith(value, " ", controls);
        }
        bool IErrorAlert.TextStartsWith(string value, string message, params System.Windows.Forms.Control[] controls)
        {
            bool result;

            result = true;
            foreach (System.Windows.Forms.Control control in controls)
            {
                if (!TextStartsWith(value, message, control))
                    result = false;
            }

            return result;
        }
        bool TextStartsWith(string value, string message, System.Windows.Forms.Control control)
        {
            bool result;

            result = control.Text.StartsWith(value);

            if (!result)
                ((IErrorAlert)this).SetError(control, message.Translate());

            return result;
        }

        bool IErrorAlert.ValueEqual(decimal value, params System.Windows.Forms.Control[] numericUpDowns)
        {
            return ((IErrorAlert)this).ValueEqual(value, " ", numericUpDowns);
        }
        bool IErrorAlert.ValueEqual(decimal value, string message, params System.Windows.Forms.Control[] numericUpDowns)
        {
            bool result;

            result = true;
            foreach (NumericUpDown numericUpDown in numericUpDowns)
            {
                if (!ValueEqual(value, message, numericUpDown))
                    result = false;
            }

            return result;
        }
        bool ValueEqual(decimal value, string message, System.Windows.Forms.Control numericUpDown)
        {
            bool result;

            if (numericUpDown is NumericUpDown)
                result = ((NumericUpDown)numericUpDown).Value == value;
            else
                throw new AtomusException("Not Support Control.");

            if (!result)
                ((IErrorAlert)this).SetError(numericUpDown, message.Translate());

            return result;
        }

        bool IErrorAlert.ValueGreaterThan(decimal value, params System.Windows.Forms.Control[] numericUpDowns)
        {
            return ((IErrorAlert)this).ValueGreaterThan(value, " ", numericUpDowns);
        }
        bool IErrorAlert.ValueGreaterThan(decimal value, string message, params System.Windows.Forms.Control[] numericUpDowns)
        {
            bool result;

            result = true;
            foreach (NumericUpDown numericUpDown in numericUpDowns)
            {
                if (!ValueGreaterThan(value, message, numericUpDown))
                    result = false;
            }

            return result;
        }
        bool ValueGreaterThan(decimal value, string message, System.Windows.Forms.Control numericUpDown)
        {
            bool result;

            if (numericUpDown is NumericUpDown)
                result = ((NumericUpDown)numericUpDown).Value > value;
            else
                throw new AtomusException("Not Support Control.");

            if (!result)
                ((IErrorAlert)this).SetError(numericUpDown, message.Translate());

            return result;
        }

        bool IErrorAlert.ValueGreaterThanOrEqual(decimal value, params System.Windows.Forms.Control[] numericUpDowns)
        {
            return ((IErrorAlert)this).ValueGreaterThanOrEqual(value, " ", numericUpDowns);
        }
        bool IErrorAlert.ValueGreaterThanOrEqual(decimal value, string message, params System.Windows.Forms.Control[] numericUpDowns)
        {
            bool result;

            result = true;
            foreach (NumericUpDown numericUpDown in numericUpDowns)
            {
                if (!ValueGreaterThanOrEqual(value, message, numericUpDown))
                    result = false;
            }

            return result;
        }
        bool ValueGreaterThanOrEqual(decimal value, string message, System.Windows.Forms.Control numericUpDown)
        {
            bool result;

            if (numericUpDown is NumericUpDown)
                result = ((NumericUpDown)numericUpDown).Value >= value;
            else
                throw new AtomusException("Not Support Control.");

            if (!result)
                ((IErrorAlert)this).SetError(numericUpDown, message.Translate());

            return result;
        }

        bool IErrorAlert.ValueLessThan(decimal value, params System.Windows.Forms.Control[] numericUpDowns)
        {
            return ((IErrorAlert)this).ValueLessThan(value, " ", numericUpDowns);
        }
        bool IErrorAlert.ValueLessThan(decimal value, string message, params System.Windows.Forms.Control[] numericUpDowns)
        {
            bool result;

            result = true;
            foreach (NumericUpDown numericUpDown in numericUpDowns)
            {
                if (!ValueLessThan(value, message, numericUpDown))
                    result = false;
            }

            return result;
        }
        bool ValueLessThan(decimal value, string message, System.Windows.Forms.Control numericUpDown)
        {
            bool result;

            if (numericUpDown is NumericUpDown)
                result = ((NumericUpDown)numericUpDown).Value < value;
            else
                throw new AtomusException("Not Support Control.");

            if (!result)
                ((IErrorAlert)this).SetError(numericUpDown, message.Translate());

            return result;
        }

        bool IErrorAlert.ValueLessThanOrEqual(decimal value, params System.Windows.Forms.Control[] numericUpDowns)
        {
            return ((IErrorAlert)this).ValueLessThanOrEqual(value, " ", numericUpDowns);
        }
        bool IErrorAlert.ValueLessThanOrEqual(decimal value, string message, params System.Windows.Forms.Control[] numericUpDowns)
        {
            bool result;

            result = true;
            foreach (NumericUpDown numericUpDown in numericUpDowns)
            {
                if (!ValueLessThanOrEqual(value, message, numericUpDown))
                    result = false;
            }

            return result;
        }
        bool ValueLessThanOrEqual(decimal value, string message, System.Windows.Forms.Control numericUpDown)
        {
            bool result;

            if (numericUpDown is NumericUpDown)
                result = ((NumericUpDown)numericUpDown).Value <= value;
            else
                throw new AtomusException("Not Support Control.");

            if (!result)
                ((IErrorAlert)this).SetError(numericUpDown, message.Translate());

            return result;
        }
    }
}
