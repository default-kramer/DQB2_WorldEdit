using Godot;
using System;

// TODO
namespace EyeOfRubiss.Nodes
{
    public partial class TimeRange : Godot.Range
    {
        private LineEdit _LineEdit { get; set; }
        private Button _ButtonUp { get; set;}
        private Button _ButtonDown { get; set; }
    
        public override void _Ready()
        {
            _OnReadyVariables();
    
            _UpdateButtonsDisabled();
        }
        private void _OnReadyVariables()
        {
            _LineEdit = GetNode<LineEdit>("LineEdit");
            _ButtonUp = GetNode<Button>("ButtonUp");
            _ButtonDown = GetNode<Button>("ButtonDown");
        }
    
        private void _UpdateButtonsDisabled()
        {
            _ButtonUp.Disabled = Value >= MaxValue;
            _ButtonDown.Disabled = Value <= MinValue;
        }
    
        public void _On_Self_Changed()
        {
            GD.Print("I've been changed.");
        }
        public void _On_Self_ValueChanged(float value)
        {
            GD.Print($"My own value was changed to {value}.");
            _UpdateButtonsDisabled();
            _LineEdit.Text = value.ToString();
        }
    
        public void _On_LineEdit_EditingToggled(bool toggledOn)
        {
            GD.Print($"LineEdit: Editing was toggled {(toggledOn ? "on" : "off")}.");
        }
        public void _On_LineEdit_TextChangeRejected(string rejectedSubstring)
        {
            GD.Print($"LineEdit: Text change rejected. Rejected substring: {rejectedSubstring}");
        }
        public void _On_LineEdit_TextChanged(string newText)
        {
            GD.Print($"LineEdit: Text changed. New text: {newText}");
        }
        public void _On_LineEdit_TextSubmitted(string newString)
        {
            GD.Print($"LineEdit: Text submitted. New text: {newString}");
        }
    
        public void _On_ButtonUp_ButtonDown()
        {
            GD.Print("Up Button: Button is down.");
            Value += Step;
        }
        public void _On_ButtonUp_ButtonUp()
        {
            GD.Print("Up Button: Button is up.");
        }
        public void _On_ButtonUp_Pressed()
        {
            GD.Print("Up Button: Button was pressed.");
        }
    
        public void _On_ButtonDown_ButtonDown()
        {
            GD.Print("Down Button: Button is down.");
            Value -= Step;
        }
        public void _On_ButtonDown_ButtonUp()
        {
            GD.Print("Down Button: Button is up.");
        }
        public void _On_ButtonDown_Pressed()
        {
            GD.Print("Down Button: Button was pressed.");
        }
    }
}
