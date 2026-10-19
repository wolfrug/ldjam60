// Functions

LIST characters = Deya, Zayne
LIST emotions = Angry, Neutral1, Neutral2, Sad, Shock, Smile1, Smile2, Smirk1, Smirk2

LIST oldTek =  FlipPhone,IPod, Walkman, CDPlayer, Camera, CellPhone, BoomBox, ReelRecorder, VHS, CassetteRecorder, Pager, FaxMachine, Calculator, HDD, CardReader, BarcodeScanner, PaperCutter, CashRegister,FloppyDisk, CD, CRTMonitor, Joystick, Printer, VinylPlayer, Radio,TV,TVRemote,SlideProjector,ArcadeMachine,Clock

VAR pickup = 0

==toDoList
#ToDoList^true #clear
To do:[br][br]#List

<>Collect 1 x Flip Phone.[br] #List
<>Collect 1 x Walkman.[br] #List
<>Collect 2 x Floppy Disks.[br] #List
<>Collect 1 x Joystick.[br][br] #List

<>Thank you![br] #List

<>Your benefactor,[br]The Collector #List

#ToDoList^false
->->

===function Pickup(id, amount)
~pickup+=amount
We received {id} and {amount}, pickup is now: {pickup}
~return "Rad"

===function ClearPortraits()
#SET_PORTRAIT_LEFT:None
#SET_PORTRAIT_RIGHT:None

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