﻿using System;
using System.Linq;
using System.Speech.Recognition;
using System.Speech.Synthesis;
using System.Threading;
using System.Threading.Tasks;
using ExtPlaneNet;
using ExtPlaneNet.Commands;

namespace X_Plane_Voice_Control.Commands
{
    class VertticalSpeedControl : ControlTemplate
    {
        private readonly string[] _lnavOnStrings = { "select" };
        private readonly string[] _lnavOffStrings = { "de-select" };
        public VertticalSpeedControl(ExtPlaneInterface interface_, SpeechSynthesizer synthesizer) : base(interface_, synthesizer)
        {
            var lnavGrammar = new GrammarBuilder();
            var lnavGrammarOn = new GrammarBuilder();
            lnavGrammarOn.Append(new Choices(_lnavOnStrings));
            var lnavGrammarOff = new GrammarBuilder();
            lnavGrammarOff.Append(new Choices(_lnavOffStrings));
            lnavGrammar.Append(new Choices(lnavGrammarOn, lnavGrammarOff, "toggle"));
            lnavGrammar.Append("vertical speed");
            lnavGrammar.Append("mode", 0, 1);
            Grammar = new Grammar(lnavGrammar);
            RecognitionPattern = Constants.DeserializeRecognitionPattern(lnavGrammar.DebugShowPhrases);
        }

        public sealed override Grammar Grammar { get; }
        public override string RecognitionPattern { get; }

        public override void DataRefSubscribe()
        {
            XPlaneInterface.Subscribe<double>("laminar/B738/autopilot/vs_status");
        }

        public override void OnTrigger(RecognitionResult rResult, string phrase)
        {
            if (phrase.Contains("toggle"))
            {
                PressButton();
                SpeechSynthesizer.SpeakAsync("vertical speed toggled");
                return;
            }

            var turnOn = !_lnavOffStrings.Any(phrase.Contains);
            var lnavStatus = (int)XPlaneInterface.GetDataRef<double>("laminar/B738/autopilot/vs_status").Value;
            if (turnOn && lnavStatus == 0)
            {
                PressButton();
                SpeechSynthesizer.SpeakAsync("vertical speed engaged");
            }
            else if (!turnOn && lnavStatus == 1)
            {
                PressButton();
                SpeechSynthesizer.SpeakAsync("vertical speed disengaged");
            }
        }

        private void PressButton()
        {
            Task.Run(() =>
            {
                XPlaneInterface.SetExecutingCommand("laminar/B738/autopilot/vs_press", Command.CommandType.Begin);
                Thread.Sleep(Constants.PushButtonReleaseDelay);
                XPlaneInterface.SetExecutingCommand("laminar/B738/autopilot/vs_press", Command.CommandType.End);
            });
        }
    }
}