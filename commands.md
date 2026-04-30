# These are the Eelink tracker commands, section tiltes below are their names, then there are descriptions and samples

## Factory Reset Defaults

Values returned by the device after `FACTORY#`:

| Command | Factory Default |
|---|---|
| `GPS?#` | `GPS:1,120,0,0,0,0,648` |
| `GSM?#` | `GSM:0,120,0,0,0,0,648` |
| `COLLECT?` | `COLLECT:60,0,0,0,1` |
| `MOTION?` | `MOTION:0,0` |
| `SPEED?` | `SPEED:0.00,0.00,0.00` |
| `FENCE,0?` | `FENCE1:NONE FENCE2:NONE FENCE3:NONE FENCE4:NONE FENCE5:NONE FENCE6:NONE FENCE7:NONE FENCE8:NONE` |

## COLLECT

This command requests to change the parameters of location collection. All parameters define how to collect location package and how many location packages to be cached before they are sent to server. We have 3 strategies to collect location package. The first strategy is based on time. The location packages are generated in specific interval. There are 2 intervals: [INTERVAL] and [ACTIVE]. [INTERVAL] defines the regular interval. [ACTIVE] defines the time interval when device is active/moving. The second strategy is based on position. After device moves a specific distance, a location package will be generated. [DISTANCE] defines the gap. The third strategy is based on course. When device turns more than a specific angle, a location package will be generated. [TURN] defines the angle. Every strategy can be omitted if its parameter is set to 0. If multiply strategies are set, a location packages will be generated when any one is met.

### Usage
```
COLLECT,[INTERVAL],[DISTANCE],[TURN],[ACTIVE],[QUANTITY]#
COLLECT?
```

### Parameters
| Param | Description |
|---|---|
| INTERVAL | The time interval (in seconds) |
| DISTANCE | The running distance (in meters) |
| TURN | The turning angle (in degrees) |
| ACTIVE | The time interval when device is moving/active (in seconds) |
| QUANTITY | The number of cached location packages before they are sent |

---

## GPS

GPS module has 2 states: ON and OFF. When GPS module is OFF, GPS chip is closed. Normally, we can use this command to save a lot of power consumption. This command requests to change the work mode and parameters of GPS module. It defines how and when GPS module switches in 2 states.

If [MODE] is 0, GPS module will be always ON and other parameters are omitted. If [MODE] is 3, GPS module will be always OFF and other parameters are omitted. If [MODE] is 1 or 2, GPS module will be ON based on the timer defined with other parameters. There are 2 phases to be defined. At first, GPS module enters phase 1 when the command is executed. Then it enters phase 2 after phase 1 is finished. Phase 1 will run for [T1_TOTAL] minutes. In phase 1, GPS module will be ON for [T1_WAKING] minutes and then be OFF in remaining time. Phase 2 is periodic in [T2_PERIODIC] minutes. In each period of phase 2, GPS module will be ON for [T2_WAKING] minutes and then be OFF in remaining time. If [MODE] is 1, GPS module will be ON if it is active besides the above timers. The activity is detected by accelerometer.

### Usage
```
GPS,[MODE],[T0],[T1_TOTAL],[T1_WAKING],[T2_PERIODIC],[T2_WAKING]#
GPS?
```

### Parameters
| Param | Description |
|---|---|
| MODE | The work mode — 0: ALWAYS ON, 1: AUTOMATIC, 2: ON TIMERS, 3: ALWAYS OFF |
| T0 | The work time after GPS module is woken up (in seconds) |
| T1_TOTAL | The total time of phase 1 (in minutes) |
| T1_WAKING | The work time in phase 1 (in minutes) |
| T2_PERIODIC | The periodic time of phase 2 (in minutes) |
| T2_WAKING | The work time in phase 2 (in minutes) |
| GPS_RUN | (query only) The running time from last GPS command (in minutes) |

### Examples
| Command | Effect |
|---|---|
| `GPS,0#` | GPS module is always ON. |
| `GPS,3#` | GPS module is always OFF. |
| `GPS,1#` | GPS module is ON when device is active. |
| `GPS,1,90,0,0,60,5#` | GPS module is ON for 5 minutes per 60 minutes or when device is active. |
| `GPS,2,90,0,0,60,5#` | GPS module is ON for 5 minutes per 60 minutes. |
| `GPS,2,90,120,120,60,5#` | GPS module is ON for 120 minutes at first. Then it is ON for 5 minutes per 60 minutes. |

**Note:**
1. The data collection period can be synchronized with GPS on/off period. If so, there is an extra mechanism to reduce the power consumption. In such situation, once the device locates the position of itself in phase 2, the GPS module can be OFF in advance. The running time will be less than [T0] and [T2_WAKING].
2. If the device has a WiFi chip, then some extra [MODE] can be supported:
   - If [MODE] is 1 or 2, both GPS module and WiFi chip will be ON during each GPS cycle. The other behaviour is same as the above corresponding description.
   - If [MODE] is 11 or 12 (WiFi first), both GPS module and WiFi chip will be ON during each GPS cycle. Then GPS module will be OFF if two or more WiFi hotspots are scanned. The other behaviour is same as the above corresponding description.
   - If [MODE] is 21 or 22 (WiFi only), GPS module will be OFF and only WiFi chip is used during each GPS cycle. The other behaviour is same as the above corresponding description.

---

## GSM

Controls when the GSM/LTE modem is on or off. Turning it off saves significant power.

### Usage
```
GSM,[MODE],[T0],[T1_TOTAL],[T1_WAKING],[T2_PERIODIC],[T2_WAKING]#
GSM?
```

### Parameters
| Param | Description |
|---|---|
| MODE | 0: Always ON, 1: Automatic (accelerometer), 2: On timers, 3: Always OFF |
| T0 | Work time after GSM wakes up (seconds) |
| T1_TOTAL | Total duration of phase 1 (minutes) |
| T1_WAKING | GSM on-time within phase 1 (minutes) |
| T2_PERIODIC | Phase 2 repeat interval (minutes) |
| T2_WAKING | GSM on-time per phase 2 cycle (minutes) |
| GSM_RUN | (query only) Running time since last GSM command (minutes) |

In MODE 1/2, device runs phase 1 first (for T1_TOTAL minutes), then repeats phase 2 forever. In MODE 1, GSM also turns on when the accelerometer detects activity.

### Examples
| Command | Effect |
|---|---|
| `GSM,0#` | Always on |
| `GSM,3#` | Always off (flight mode) |
| `GSM,1#` | On when active (accelerometer-driven) |
| `GSM,1,90,0,0,60,5#` | On 5 min/hour or when active |
| `GSM,2,90,0,0,60,5#` | On 5 min/hour, timer only |
| `GSM,2,90,120,120,60,5#` | On for 120 min at start, then 5 min/hour |

**Note:**
1. The data collection period can be synchronized with GSM on/off period. If so, there is an extra mechanism to reduce the power consumption. In such situation, if the device is registered into the network and completes to transfer all pending data in phase 2, the GSM module can be OFF in advance. The running time will be less than [T0] and [T2_WAKING].

---

## MOTION

This command requests to enable/disable motion warning and set its parameter. After motion warning is enabled, any vibration will trigger a warning.

### Usage
```
MOTION,[SENSE],[DELAY]#
MOTION?
```

### Parameters
| Param | Description |
|---|---|
| SENSE | The sensitivity |
| DELAY | The delay time before a warning is emitted (in seconds) |

**[SENSE]:**
- `0`: Disable warning.
- `1` ~ `9`: Enable warning. 1 is the most sensitive, and 9 is the least sensitive.

### Examples
| Command | Description |
|---|---|
| `MOTION#` | Disable motion warning |
| `MOTION,2,5#` | Trigger motion warning when an enough vibration continues 5 |

---

## SPEED

This command requests to enable/disable speed warning and set its parameter. After speed warning is enabled, any speed not in range will trigger a warning.

### Usage
```
SPEED,[LOW],[HIGH],[OVER]#
SPEED?
```

### Parameters
| Param | Description |
|---|---|
| LOW | The low limit of the speed (in km/h) |
| HIGH | The high limit of the speed (in km/h) |
| OVER | The speed threshold (in km/h) over which the device will drive the relay |

**Notes:**
1. When [LOW] is 0, the under-speed warning is disabled.
2. When [HIGH] is 0, the over-speed warning is disabled.
3. When [OVER] is 0, the speed-relay feature is disabled.

### Examples
| Command | Description |
|---|---|
| `SPEED#` | Disable speed warning |
| `SPEED,30,0#` | Enable under-speed warning when speed is less than 30km/h |
| `SPEED,0,100#` | Enable over-speed warning when speed is more than 100km/h |
| `SPEED,30,100#` | Enable both under-speed warning and over-speed warning |
| `SPEED,0,0,80#` | Drive the relay when the speed is over 80 km/h |

---

## FENCE

This command requests to add/remove/modify one or more fences in device. Up to 8 fences can be added into device. Each fence can be round or rectangle. And it can also be out-type, in-type or bidirectional. If it is a out-type, the outside of fence is banned. If device leaves it, an out-of-fence warning will be triggered. If it is a in-type, the inside of fence is banned. If device enters it, an in-to-fence warning will be triggered. If it is bidirectional, any action to cross the border will trigger a warning.

### Usage
```
FENCE,[INDEX],[FLAG],[LNG0],[LAT0],[RADIUS]#        (round fence)
FENCE,[INDEX],[FLAG],[LNG1],[LAT1],[LNG2],[LAT2]#   (rectangle fence)
FENCE,[INDEX]#                                       (remove fence)
FENCE,[INDEX]?                                       (query fence)
```

### Parameters
| Param | Description |
|---|---|
| INDEX | The index of fence — Integer, 0 - 8 |
| FLAG | The type and shape of fence — String, each char represents an attribution |
| LNG0, LAT0 | The longitude and latitude of the center of round fence |
| RADIUS | The radius of round fence (in meters) |
| LNG1, LAT1 | The longitude and latitude of the left-top corner of rectangle fence |
| LNG2, LAT2 | The longitude and latitude of the right-bottom corner of rectangle fence |

**Notes:**
1. When [INDEX] is 0, it means a command to all fences.
2. When [LNG0] and [LAT0] are empty, we use last fixed position.

**[FLAG]:**
- `N/A`: Fence is disabled
- `O`: Out-type fence
- `I`: In-type fence
- `C`: Bidirectional fence
- `R`: Round fence
- `S`: Rectangle fence

### Examples
| Command | Description |
|---|---|
| `FENCE,1,OR,,,500#` | Setup fence 1 (out-type, round) round last fixed position |
| `FENCE,1,IR,,,500#` | Setup fence 1 (in-type, round) round last fixed position |
| `FENCE,1,CR,,,500#` | Setup fence 1 (bidirectional, round) round last fixed position |
| `FENCE,1,OR,113.5,22.5,500#` | Setup fence 1 (out-type, round) round specific position |
| `FENCE,1,IR,113.5,22.5,500#` | Setup fence 1 (in-type, round) round specific position |
| `FENCE,1,CR,113.5,22.5,500#` | Setup fence 1 (bidirectional, round) round specific position |
| `FENCE,1,OS,113.2,22.2,113.8,22.8#` | Setup fence 1 (out-type, rectangle) |
| `FENCE,1,IS,113.2,22.2,113.8,22.8#` | Setup fence 1 (in-type, rectangle) |
| `FENCE,1,CS,113.2,22.2,113.8,22.8#` | Setup fence 1 (bidirectional, rectangle) |
| `FENCE,1#` | Remove the first fence |
| `FENCE,0#` | Remove all fences |
| `FENCE,1?` | Return the first fence |
| `FENCE,0?` | Return all fences |

