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
#include "Maximilian.h"

class SynthVoice : public SynthesiserVoice
{

public:

    SynthVoice()
    {
        *frequency = frequencyOfA;
    };

    SynthVoice(AudioParameterFloat* level)
    {
        *frequency = frequencyOfA;
        *gain = *level;
    };

    bool canPlaySound (SynthesiserSound* sound)
    {
        return dynamic_cast<SynthSound*>(sound) != nullptr;
    }

    void startNote (int midiNoteNumber, float velocity, SynthesiserSound* sound, int currentPitchWheelPosition)
    {
        *frequency = MidiMessage::getMidiNoteInHertz(midiNoteNumber);
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
        
        

        for (int sample = 0; sample < numSamples; ++sample)
        {
            
            double theWave = osc1.sinewave(frequencyOfA) * *gain;

            for (int channel = 0; channel < outputBuffer.getNumChannels(); ++channel)
            {
                outputBuffer.addSample(channel, startSample, theWave);
            }
            ++startSample;
        }
    }


private:
    AudioParameterFloat* frequency;
    const double frequencyOfA = 440;
    float* gain;

    maxiOsc osc1;
    
};