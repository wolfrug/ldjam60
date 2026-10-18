// Functions!

LIST small_bots = SmallBot1 = 1, SmallBot2 = 2, SmallBot3 = 3, SmallBot4 = 4
LIST medium_bots = MediumBot1 = 5, MediumBot2 = 6
LIST large_bots = LargeBot1 = 7

LIST attributes = NoAttribute, Strength, Dexterity, Consitution, Intelligence, Wisdom, Charisma

LIST slots = NoSlot, Slot1_Empty, Slot2_Empty, Slot3_Empty, Slot4_Empty

LIST wheels = Tread, ATV, Walker, Hover

LIST eq_strength = STR_Excavator = 1

LIST eq_dexterity = DEX_RepairKit = 2

LIST eq_constitution = CON_SmallPowerCell = 3

LIST eq_intelligence = INT_WaterLab = 4

LIST eq_wisdom = WIS_Scanner = 5

LIST eq_charisma = CHA_Chatbot = 6

LIST eq_misc = NoEquipment = 0, MISC_SecurityBot = 7

LIST bot_charge = Broken = -1, NoPower = 0, LowPower = 1, MediumPower = 2, HighPower = 3, FullPower = 4

LIST task_progress = TaskProgress0 = 0,TaskProgress1 = 1,TaskProgress2 = 2,TaskProgress3 = 3,TaskProgress4 = 4,TaskProgress5 = 5,TaskProgress6 = 6,TaskProgress7 = 7,TaskProgress8 = 8,TaskProgress9 = 9,TaskProgress10 = 10

LIST task_max_progress = TaskMaxProgress0 = 0,TaskMaxProgress1 = 1,TaskMaxProgress2 = 2,TaskMaxProgress3 = 3,TaskMaxProgress4 = 4,TaskMaxProgress5 = 5,TaskMaxProgress6 = 6,TaskMaxProgress7 = 7,TaskMaxProgress8 = 8,TaskMaxProgress9 = 9,TaskMaxProgress10 = 10

LIST task_states = TaskNotStarted, TaskStarted, TaskFailed, TaskStalled, TaskStuck, TaskComplete

LIST task_consequences = TaskControlled, TaskRisky, TaskDesperate

LIST task_progress_success = NegativeProgress = -1, NoProgress = 0, LimitedProgress = 1, NormalProgress = 2, GreatProgress = 3, CriticalProgress = 5

LIST tasks = NoTask, Task_00_BotBay, Task_01_Earthquake

VAR allEquipment = ()
VAR allBots = ()
VAR ownedBots = ()
VAR ownedEquipment = ()

VAR smallbot1 = ()
VAR smallbot2 = ()
VAR smallbot3 = ()
VAR smallbot4 = ()
VAR mediumbot1 = ()
VAR mediumbot2 = ()
VAR largebot1 = ()

VAR selectedBot = ()
VAR selectedEquipment = ()
VAR selectedTask = ()
VAR returnSpot = ()

VAR currentTasks = ()
VAR completedTasks = ()
VAR failedTasks = ()

VAR task_botbay_00 = ()
VAR task_earthquake_01 = ()


===function IncreaseTaskRisk(task)===
~temp targetTask = GetTask(task)
~temp currentRisk = targetTask^LIST_ALL(task_consequences)
{LIST_COUNT(currentRisk)>0:
{currentRisk:
- TaskControlled:
~targetTask-=TaskControlled
~targetTask+=TaskRisky
- TaskRisky:
~targetTask-=TaskRisky
~targetTask+=TaskDesperate
}
- else:
~targetTask+=TaskControlled
}
{SetTask(task, targetTask)}

===function ReduceTaskRisk(task)===
~temp targetTask = GetTask(task)
~temp currentRisk = targetTask^LIST_ALL(task_consequences)
{LIST_COUNT(currentRisk)>0:
{currentRisk:
- TaskControlled:
~targetTask-=TaskControlled
- TaskRisky:
~targetTask-=TaskRisky
~targetTask+=TaskControlled
- TaskDesperate:
~targetTask-=TaskDesperate
~targetTask+=TaskRisky
}
}
{SetTask(task, targetTask)}

===function GetTaskRisk(task)
~temp targetTask = GetTask(task)
~temp currentRisk = targetTask^LIST_ALL(task_consequences)
{LIST_COUNT(currentRisk)<0:
~return TaskNotStarted
- else:
~return currentRisk
}

===function SetTaskRisk(task, risk)
~temp targetTask = GetTask(task)
~temp currentRisk = targetTask^LIST_ALL(task_consequences)
~targetTask -= currentRisk
~targetTask += risk
~SetTask(task, targetTask)

===function IncreaseTaskProgress(task, amount)
{amount>0:
~temp targetTask = GetTask(task)
~temp currentProgress = targetTask^LIST_ALL(task_progress)
~temp currentMaxProgress = targetTask^LIST_ALL(task_max_progress)
~targetTask-=TaskNotStarted
{LIST_COUNT(currentProgress)>0:
{LIST_VALUE(currentProgress)<LIST_VALUE(currentMaxProgress):
~targetTask-=currentProgress
~currentProgress++
~targetTask+=currentProgress
~amount--
- else:
~amount = 0
}
- else:
~targetTask+=TaskProgress0
}
{LIST_VALUE(currentProgress)==LIST_VALUE(currentMaxProgress):
~targetTask+=TaskComplete
}
~SetTask(task, targetTask)
~IncreaseTaskProgress(task, amount)
}

===function ReduceTaskProgress(task, amount)
{amount>0:
~temp targetTask = GetTask(task)
~temp currentProgress = targetTask^LIST_ALL(task_progress)
{LIST_COUNT(currentProgress)>0:
{LIST_VALUE(currentProgress)>LIST_VALUE(LIST_MIN(LIST_ALL(task_progress))):
~targetTask-=currentProgress
~currentProgress--
~targetTask+=currentProgress
}
~amount--
- else:
~targetTask+=TaskProgress0
}
~SetTask(task, targetTask)
~ReduceTaskProgress(task, amount)
}

===function GetTaskProgress(task)
~temp targetTask = GetTask(task)
~temp currentProgress = targetTask^LIST_ALL(task_progress)
{LIST_COUNT(currentProgress)<=0:
~return TaskNotStarted
- else:
~return currentProgress
}

===function SetTaskProgress(task, progress)
~temp targetTask = GetTask(task)
~temp currentProgress = targetTask^LIST_ALL(task_progress)
~targetTask-=currentProgress
~targetTask+=progress
~SetTask(task, targetTask)

===function GetMaxTaskProgress(task)
~temp targetTask = GetTask(task)
~temp currentProgress = targetTask^LIST_ALL(task_max_progress)
{LIST_COUNT(currentProgress)<0:
~return TaskNotStarted
- else:
~return currentProgress
}

===function SetMaxTaskProgress(task, progress)
~temp targetTask = GetTask(task)
~temp currentProgress = targetTask^LIST_ALL(task_max_progress)
~targetTask-=currentProgress
~targetTask+=progress
~SetTask(task, targetTask)

===function IncreaseStall(task)
~temp targetTask = GetTask(task)
{not (targetTask?TaskStuck):
{targetTask?TaskStalled:
~targetTask-=TaskStalled
~targetTask+=TaskStuck
- else:
~targetTask+=TaskStalled
}
~SetTask(task, targetTask)
}

==function RemoveStall(task)
~temp targetTask = GetTask(task)
~targetTask -= (TaskStalled, TaskStuck)
~SetTask(task, targetTask)

==function HasStall(task)
~temp targetTask = GetTask(task)
~return LIST_COUNT(targetTask^(TaskStalled, TaskStuck))>0

==function IsStuck(task)
~temp targetTask = GetTask(task)
~return targetTask?TaskStuck

===function GetTaskRequirements(task)
~temp targetTask = GetTask(task)
~temp currentRequirements = targetTask^LIST_ALL(allEquipment)
{LIST_COUNT(currentRequirements)<1:
~return NoAttribute
- else:
~return currentRequirements
}

===function GetTaskBots(task)===
~temp targetTask = GetTask(task)
~return targetTask^allBots

===function SetTaskBot(task, bot)===
~temp targetTask = GetTask(task)
~targetTask+=bot
~ownedBots-=bot
~SetTask(task, targetTask)

===function RemoveTaskBot(task, bot)===
~temp targetTask = GetTask(task)
~targetTask-=bot
~ownedBots+=bot
~SetTask(task, targetTask)

==function RemoveAllTaskBots(task)
~temp targetTask = GetTask(task)
~temp taskBots = GetTaskBots(task)
~targetTask -= taskBots
~ownedBots += taskBots
~SetTask(task, targetTask)

===function GetTask(task)===
{task:
-Task_01_Earthquake:
~return task_earthquake_01
-Task_00_BotBay:
~return task_botbay_00
- else:
~return NoTask
}
===function SetTask(task, newList)
{task:
-Task_01_Earthquake:
~task_earthquake_01 = newList
-Task_00_BotBay:
~task_botbay_00 = newList
- else:
~return NoTask
}

===function AddEquipment(bot, equipment)===
~temp target = GetBot(bot)
{target?equipment:
~return NoEquipment
}
{target^(Slot1_Empty,Slot2_Empty,Slot3_Empty,Slot4_Empty):
{target?Slot1_Empty:
    ~target-=Slot1_Empty
    ~target+=equipment
    ~ownedEquipment-=equipment
}
{target?Slot2_Empty:
    ~target-=Slot2_Empty
    ~target+=equipment
    ~ownedEquipment-=equipment
}
{target?Slot3_Empty:
    ~target-=Slot3_Empty
    ~target+=equipment
    ~ownedEquipment-=equipment
}
{target?Slot4_Empty:
    ~target-=Slot4_Empty
    ~target+=equipment
    ~ownedEquipment-=equipment
}
~SetBot(bot, target)
~return equipment
- else:
~return NoSlot
}

===function RemoveEquipment(bot, equipment)===
~temp target = GetBot(bot)
{target?equipment:
~target-=equipment
{target^LIST_ALL(small_bots):
    ~target+=Slot1_Empty
    ~ownedEquipment+=equipment
}
{target^LIST_ALL(medium_bots):
    {target?Slot2_Empty:
    ~target+=Slot1_Empty
    ~ownedEquipment+=equipment
    - else:
    ~target+=Slot2_Empty
    ~ownedEquipment+=equipment
    }
}
{target^LIST_ALL(large_bots):
    {target?Slot4_Empty:
    ~target+=Slot3_Empty
    ~ownedEquipment+=equipment
    }
    {target?Slot3_Empty:
    ~target+=Slot2_Empty
    ~ownedEquipment+=equipment
    }
    {target?Slot2_Empty:
    ~target+=Slot1_Empty
    ~ownedEquipment+=equipment
    - else:
    ~target+=Slot2_Empty
    ~ownedEquipment+=equipment
    }
}
~SetBot(bot, target)
~return equipment
- else:
~return NoEquipment
}

==function RemoveAllEquipment(bot)
~temp target = GetBot(bot)
~temp targetEquipment = GetAllEquipment(bot)
{LIST_COUNT(targetEquipment)>0:
~temp targetEquipmentMin = LIST_MIN(targetEquipment)
{RemoveEquipment(bot, targetEquipmentMin): }
{RemoveAllEquipment(bot)}
}

===function IncreaseBotCharge(bot, amount)
{amount>0:
~temp targetBot = GetBot(bot)
~temp currentCharge = targetBot^LIST_ALL(bot_charge)
~temp maxCharge = LIST_MAX(LIST_ALL(bot_charge))

{LIST_COUNT(currentCharge)>0:
{LIST_VALUE(currentCharge)<LIST_VALUE(maxCharge):
~targetBot-=currentCharge
~currentCharge++
~targetBot+=currentCharge
~amount--
- else:
~amount = 0
}
- else:
~targetBot+=LowPower
}

~SetBot(bot, targetBot)
~IncreaseBotCharge(bot, amount)
}

===function ReduceBotCharge(bot, amount)
{amount>0:
~temp targetBot = GetBot(bot)
~temp currentCharge = targetBot^LIST_ALL(bot_charge)
{LIST_COUNT(currentCharge)>0:
{LIST_VALUE(currentCharge)>LIST_VALUE(LIST_MIN(LIST_ALL(bot_charge))):
~targetBot-=currentCharge
~currentCharge--
~targetBot+=currentCharge
}
~amount--
- else:
~targetBot+=NoPower
}
~SetBot(bot, targetBot)
~ReduceBotCharge(bot, amount)
}

===function GetBotCharge(bot)
~temp targetBot = GetBot(bot)
~temp currentCharge = targetBot^LIST_ALL(bot_charge)
{LIST_COUNT(currentCharge)<=0:
~return NoPower
- else:
~return currentCharge
}

===function SetBotCharge(bot, charge)
~temp targetBot = GetBot(bot)
~temp currentCharge = targetBot^LIST_ALL(bot_charge)
~targetBot-=currentCharge
~targetBot+=charge
~SetBot(bot, targetBot)

==function GetBot(botref)
{botref:
-SmallBot1:
~return smallbot1
-SmallBot2:
~return smallbot2
-SmallBot3:
~return smallbot3
-SmallBot4:
~return smallbot4
-MediumBot1:
~return mediumbot1
-MediumBot2:
~return mediumbot2
-LargeBot1:
~return largebot1
- else:
~return NoEquipment
}
===function SetBot(botref, newList)
{botref:
-SmallBot1:
~smallbot1 = newList
-SmallBot2:
~smallbot2 = newList
-SmallBot3:
~smallbot3 = newList
-SmallBot4:
~smallbot4 = newList
-MediumBot1:
~mediumbot1 = newList
-MediumBot2:
~mediumbot2 = newList
-LargeBot1:
~largebot1 = newList
- else:
~return NoEquipment
}

===function GetInventoryByContent(listref)
// Gets the string name of the inventory referenced - for external reference used by the inventory system
{listref == ownedBots:
~return "ownedBots"
}
{listref == ownedEquipment:
~return "ownedEquipment"
}
{listref == allEquipment:
~return "allEquipment"
}
{listref == selectedTask:
~return "selectedTask"
}
{listref == selectedEquipment:
~return "selectedEquipment"
}
{listref == smallbot1:
~return "smallbot1"
}
{listref == smallbot2:
~return "smallbot2"
}
{listref == smallbot3:
~return "smallbot3"
}
{listref == smallbot4:
~return "smallbot4"
}
{listref == mediumbot1:
~return "mediumbot1"
}
{listref == mediumbot2:
~return "mediumbot2"
}
{listref == largebot1:
~return "largebot1"
}
// More generic inventories
{LIST_COUNT(listref^allEquipment)>0:
~return "allEquipment"
}
{LIST_COUNT(listref^allBots)>0:
~return "allBots"
}

==function GetAllEquipment(bot)
~temp target = GetBot(bot)
~temp returnList = allEquipment
~returnList = target^allEquipment
~return returnList

===function CountEmptySlots(bot)
~temp target = GetBot(bot)
~return LIST_COUNT(target^LIST_ALL(slots))

===PickEquipment(->continuePoint, targetInventory)
//Use: ->PickEquipment(->continuePoint)
//=continuePoint(itemChosen)
~selectedEquipment = targetInventory
SET_EQUIPMENT_INVENTORY({GetInventoryByContent(targetInventory)}) #updateInventory #openEquipmentInventory #wait.0.1 #continue
~temp selectedItem = ()
~temp copyList = targetInventory + NoEquipment
<-loop(continuePoint, copyList)

+ [USE_ITEM(Cancel)Cancel]
~selectedItem = NoEquipment
#hideInventory
->continuePoint(selectedItem)

=loop(->continuePoint, copyList)
{LIST_COUNT(copyList)>0:
~temp copyListMax = LIST_MIN(copyList)
~copyList-=copyListMax
<-addItem(LIST_MIN(copyList), continuePoint)
->loop(continuePoint, copyList)
}

=addItem(targetItem, ->continuePoint)
+ {allEquipment?targetItem} [USE_ITEM({targetItem}){targetItem}!]
#hideInventory #continue
->continuePoint(targetItem)

===PickBot(->continuePoint, targetInventory)
//Use: ->PickBot(->continuePoint)
//=continuePoint(itemChosen)
SET_BOT_INVENTORY({GetInventoryByContent(targetInventory)}) #updateInventory #openBotInventory #wait.0.1 #continue
~temp selectedItem = ()
~temp copyList = targetInventory + NoEquipment
<-loop(continuePoint, copyList)

+ [USE_ITEM(Cancel)Cancel]
~selectedItem = NoEquipment
#hideInventory
->continuePoint(selectedItem)

=loop(->continuePoint, copyList)
{LIST_COUNT(copyList)>0:
~temp copyListMax = LIST_MIN(copyList)
~copyList-=copyListMax
<-addItem(LIST_MIN(copyList), continuePoint)
->loop(continuePoint, copyList)
}

=addItem(targetItem, ->continuePoint)
+ {allBots?targetItem} [USE_ITEM({targetItem}){targetItem}!]
#hideInventory #continue
~selectedBot = targetItem
->continuePoint(targetItem)

==InspectBot(->returnPoint)
~returnSpot = returnPoint

->PickBot(->inspect, ownedBots)


=inspect(bot)
{bot?NoEquipment:
->returnSpot
}
Bot is {GetBot(bot)}

+ [Back]
->returnSpot

==UnequipBot(->returnPoint)
~returnSpot = returnPoint

->PickBot(->selectEquipment, ownedBots)


=selectEquipment(bot)
{bot?NoEquipment:
<color=red>No bot selected.</color> 
->returnSpot
}
~selectedBot = bot
->PickEquipment(->unequip, GetAllEquipment(bot))

=unequip(equipment)
{equipment?NoEquipment:
<color=red>No equipment selected.</color> 
->returnSpot
}
{RemoveEquipment(selectedBot, equipment): }

->returnSpot

==UnequipAllBot(->returnPoint)
~returnSpot = returnPoint

->PickBot(->unequip, ownedBots)


=unequip(bot)
{bot?NoEquipment:
->returnSpot
}

{RemoveAllEquipment(bot): }
->returnSpot

==EquipBot(->returnPoint)
~returnSpot = returnPoint

->PickBot(->selectEquipment, ownedBots)


=selectEquipment(bot)
{bot?NoEquipment:
<color=red>No bot selected.</color> 
->returnSpot
}
~selectedBot = bot

->PickEquipment(->finish, ownedEquipment)

=finish(equipment)
{equipment?NoEquipment:
<color=red>No equipment selected.</color> 
->returnSpot
}

{AddEquipment(selectedBot, equipment):
-NoSlot:
<color=red>Could not add equipment: no free slots!</color> 
-NoEquipment:
<color=red>Equipment is already equipped!.</color> 
}

->returnSpot

==AssignBotsToTask(task)
~selectedTask = task
+ [Assign Bot]
->PickBot(->assignBot, ownedBots)
+ {LIST_COUNT(GetTaskBots(task))>0} [Unassign Bot]
->PickBot(->unassignBot, GetTaskBots(task))
+ [Continue]
->->

=assignBot(bot)
{SetTaskBot(selectedTask, bot)}
->AssignBotsToTask(selectedTask)

=unassignBot(bot)
{RemoveTaskBot(selectedTask, bot)}
->AssignBotsToTask(selectedTask)

==ProgressTask(task)===
// Not a current task? We ignore
{not (task?currentTasks) || task?completedTasks || task?failedTasks:
    Task is not a current task.
    ->->
}
// Get task
~temp targetTask = GetTask(task)
// Get bots attached to task
~temp taskBots = GetTaskBots(task)
~temp functionalTaskBots = ()
// Get requirements for task
~temp taskRequirements = GetTaskRequirements(task)
// Get all available equipment
~temp availableEquipment = ()
// When we need a random bot
~temp randomBot = ()
Taskbots are: {taskBots} while requirements are {taskRequirements}. List count for taskbots is: {LIST_COUNT(taskBots)}
{LIST_COUNT(taskBots)>0:
We have taskbots.
->loop
- else:
We do not have taskbots.
->offLoop
}
- (loop)
{LIST_COUNT(taskBots)<1: ->offLoop}
Reached loop.
~temp taskBot = LIST_MIN(taskBots)
Got a temp taskbot: {taskBot}
{GetBotCharge(taskBot)>NoPower:
    ~availableEquipment += GetAllEquipment(taskBot)
    
    // Reduce their power by 1, as is default
    ~ReduceBotCharge(taskBot, 1)
    // If they still have power, add them to the list of functional bots
    {GetBotCharge(taskBot)>NoPower:
        ~functionalTaskBots+=taskBot
    }
}

~taskBots-=taskBot

//{LIST_COUNT(taskBots)>0: ->loop}
- (offLoop)

We got off the loop.

What is happening.

Overlap: {LIST_COUNT(availableEquipment^taskRequirements)} Half of requirements: {LIST_COUNT(taskRequirements)/2}

Taskbots are: {functionalTaskBots} while requirements are {taskRequirements}

// Get overlap
~temp equipment_requirement_overlap = availableEquipment^taskRequirements
// Progress bonus from bots
~temp progressBonus = 0
// Perfect overlap
{LIST_COUNT(equipment_requirement_overlap) == LIST_COUNT(taskRequirements):
    ~progressBonus++
    // Removes stuck
    {IsStuck(task):
        {RemoveStall(task)}
    }
}
// At least half matches
{LIST_COUNT(equipment_requirement_overlap)>= LIST_COUNT(taskRequirements)*0.5:
    ~progressBonus++
    // Removes stall

    {IsStuck(task):
        {RemoveStall(task)}
        // But costs one random bot 1 charge
        ~randomBot = LIST_RANDOM(functionalTaskBots)
        ~ReduceBotCharge(randomBot, 1)
        // If it goes to unpowered, remove it from list
        {GetBotCharge(randomBot)<=NoPower:
            ~functionalTaskBots-=randomBot
        }
    }
- else:
    // No matches/fewer than half
    ~progressBonus = 0
}

Progress bonus after requirement check is {progressBonus}. Requirement overlap is {equipment_requirement_overlap}.

~temp randomNumber = RANDOM(-1,1)
Random number rolled was {randomNumber}
// Get regular progress without bot
{GetTaskRisk(task):
    - TaskControlled:
    // 0-1-1
        {randomNumber:
            --1:
                {progressBonus<1: // no useful robot -> stall
                    {IncreaseStall(task)}
                } // else no effect (no help either)
            -0:
                ~progressBonus++ // normal bonus
            -1:
                ~progressBonus++ // normal bonus [could be great bonus??]
        }
    - TaskRisky:
    // 0-0-1
        {randomNumber:
            - -1:
                {progressBonus<1: // no useful robot -> stall
                    {IncreaseStall(task)}
                - else: // useful robot: reduce charge, but no stall
                    ~randomBot = LIST_RANDOM(functionalTaskBots)
                    ~ReduceBotCharge(randomBot, 1)
                    // If it goes to unpowered, remove it from list
                    {GetBotCharge(randomBot)<=NoPower:
                        ~functionalTaskBots-=randomBot
                    }
                }
            - 0:
                {progressBonus<1: // no useful robot -> stall
                    {IncreaseStall(task)}
                - else:
                    ~progressBonus++
                }
            - 1:
                ~progressBonus++ // Best case scenario
        }
    - TaskDesperate:
    // -1-0-1
        {randomNumber:
            - -1:
            // Oop.
                {progressBonus<1:
                // no useful bot --> LOSE progress!!
                     ~progressBonus--
                -else:
                    // Reduce bot charge by -two- (which might break it), but no progress loss
                    ~randomBot = LIST_RANDOM(functionalTaskBots)
                    ~ReduceBotCharge(randomBot, 2)
                    // If it goes to unpowered, remove it from list
                    {GetBotCharge(randomBot)<=NoPower:
                        ~functionalTaskBots-=randomBot
                    }
                }
            - 0:
            // Stall
                {progressBonus<1: // no useful robot -> stall
                    {IncreaseStall(task)}
                }
            - 1:
            // Best case scenario
                ~progressBonus++
        }
}
// Actually progress stack
{progressBonus>0:
    {IsStuck(task) && targetTask^(TaskControlled, TaskRisky):
    // If the task is stuck but we got a bonus, progress but increase risk
        ~IncreaseTaskRisk(task)
        ~RemoveStall(task)
    }
    {not IsStuck(task):
        ~IncreaseTaskProgress(task, progressBonus)
        ~RemoveStall(task)
    }
}
// Unless we go backwards
{progressBonus<0:
    ~ReduceTaskProgress(task, progressBonus)
}
// return bots
~RemoveAllTaskBots(task)
->->
