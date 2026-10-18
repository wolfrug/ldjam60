// Functions

LIST characters = Deya, Zayne
LIST emotions = Angry, Neutral1, Neutral2, Sad, Shock, Smile1, Smile2, Smirk1, Smirk2

===function S(char, emotion)
{char?Deya:
#SET_PORTRAIT_LEFT:{char}/{emotion}.png^{char} #SET_CUSTOM_LABEL:Left
- else:
#SET_PORTRAIT_RIGHT:{char}/{emotion}.png^{char} #SET_CUSTOM_LABEL:Right
}

===function B(target, zoom, time)
#SET_POS:{target} #SET_CAMERA_TARGET:{target}^{zoom}^{time}

===function SpeechBubble(target, bubblepos, chain)
// Usage: {SpeechBubble(face_1, bubble_1, chain_1) OR leave chain_1 as "" for no chain
{chain!="":#SET_BUBBLEPOS:{target}^{chain}|#SET_BUBBLEPOS:{target}} #SET_POS:{bubblepos}^true #SET_CUSTOM_LABEL:SpeechBubble_Talk