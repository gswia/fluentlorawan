function decodeUplink(input) {
  return {
    data: Decode(input.fPort, input.bytes, input.variables)
  };
}

function Decode(fPort, bytes, variables) {
  var bat_value = (bytes[0] << 8 | bytes[1]) & 0x3FFF;
  var bat_mv = bat_value;
  
  var door_open = bytes[0] & 0x80 ? 1 : 0;
  var mod = bytes[2];
  var alarm = bytes[9] & 0x01;
  
  if (mod == 1) {
    var open_times = bytes[3] << 16 | bytes[4] << 8 | bytes[5];
    var open_duration = bytes[6] << 16 | bytes[7] << 8 | bytes[8];
    
    if (bytes.length == 10) {
      return {
        Bat_mV: bat_mv,
        Door_Open: door_open,
        Open_Times: open_times,
        Open_Duration: open_duration,
        Alarm: alarm
      };
    }
  }
  else if (mod == 3) {
    if (bytes.length == 10) {
      return {
        Bat_mV: bat_mv,
        Door_Open: door_open,
        Alarm: alarm
      };
    }
  }
  
  return {
    Bat_mV: bat_mv
  };
}