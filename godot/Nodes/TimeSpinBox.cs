using Godot;
using System;

// TODO delete this
namespace DQBEdit.Nodes
{
	public partial class TimeSpinBox : SpinBox
	{
		[Signal] public delegate void TimeChangedEventHandler();

		private LineEdit CustomLineEdit { get; set; }

	    public static float ConvertValueToTime(float value)
		{
			float time = value * 2;
			int hours = (int)Math.Floor(time / 100);
			int minutes = (int)Math.Floor(time % 100) / 100 * 60;
			return hours * 100 + minutes + value % 1;
		}
		public static float ConvertTimeToValue(float time)
		{
			float value = time / 2;
			return time / 2;
		}
		public static string GetTimeString(float value)
		{
			int timeValue = (int)Math.Floor(ConvertValueToTime(value));
			int hours = timeValue / 100;
			int minutes = timeValue % 100;
			return $"{hours:D2}:{minutes:D2}";
		}

	    public override void _Ready()
	    {
	        GetLineEdit().Hide();
			_OnReadyVariables();
	    }
		private void _OnReadyVariables()
		{
			CustomLineEdit = GetNode<LineEdit>("CustomLineEdit");
		}


		public void _OnValueChanged(float value)
		{
			value %= (float)MaxValue;
			while (value < MinValue)
				value += (float)MaxValue;

			SetValueNoSignal(value);

			CustomLineEdit.Text = GetTimeString(value);
		}
	}
}