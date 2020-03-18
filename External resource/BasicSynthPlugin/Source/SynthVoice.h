/*
  ==============================================================================

    SynthVoice.h
    Created: 16 Mar 2020 9:43:48am
    Author:  Chris

  ==============================================================================
*/

#pragma once
#include <JuceHeader.h>
#include "SynthSound.h"

class SynthVoice : public SynthesiserVoice
{
public:
    bool canPlaySound (SynthesiserSound* sound)
    {
        return dynamic_cast<SynthSound*>(sound) != nullptr;
    }

    void startNote (int midiNoteNumber, float velocity, SynthesiserSound* sound, int currentPitchWheelPosition)
    {
        frequency = MidiMessage::getMidiNoteInHertz(midiNoteNumber);
        //frequency = frequencyOfA * std::pow(2.0, (midiNoteNumber - 69) / 12.0);
        //std::cout << midiNoteNumber << std::endl;
        Logger::outputDebugString(std::to_string(midiNoteNumber));
    }

    void stopNote(float velocity, bool allowTailOff)
    {
        clearCurrentNote();
    }

    void pitchWheelMoved(int newPitchWheel)
    {
    }

    void controllerMoved(int controllerNumber, int newControllerValue)
    {
    }

    void renderNextBlock(AudioBuffer<float>& outputBuffer, int startSample, int numSamples)
    {
        // DSP CODE HERE
    }

private:
    double level;
    double frequency;
    const double frequencyOfA = 440;

};