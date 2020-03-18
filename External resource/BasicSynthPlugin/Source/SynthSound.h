/*
  ==============================================================================

    SynthSound.h
    Created: 16 Mar 2020 9:43:36am
    Author:  Chris

  ==============================================================================
*/

#pragma once
#include <JuceHeader.h>

class SynthSound : public SynthesiserSound
{
public:
    bool appliesToNote(int /*midiNoteNumber*/)
    {
        return true;
    }
    bool appliesToChannel(int /*midiChannel*/)
    {
        return true;
    }
};