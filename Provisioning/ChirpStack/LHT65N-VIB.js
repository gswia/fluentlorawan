function str_pad(byte) {
  var zero = '0';
  var hex = byte.toString(16);
  var tmp = 2 - hex.length;
  return zero.substr(0, tmp) + hex;
}

function decodeUplink(input) {
  return {
    data: Decode(input.fPort, input.bytes, input.variables)
  };
}

function datalog(i, bytes) {
  var aa = parseFloat((bytes[i] << 24 >> 16 | bytes[i + 1]) / 1000).toFixed(3);
  var bb = parseFloat((bytes[i + 2] << 24 >> 16 | bytes[i + 3]) / 1000).toFixed(3);
  var cc = parseFloat((bytes[i + 4] << 24 >> 16 | bytes[i + 5]) / 1000).toFixed(3);
  var string = '[(' + aa + '),' + '(' + bb + '),' + '(' + cc + ')]' + ',';
  return string;
}

function Decode(fPort, bytes, variables) {
  var decode = {};

  if (fPort == 2) {
    decode.Bat_mV = bytes[0] << 8 | bytes[1];
    var mod = (bytes[2] >> 2) & 0x07;

    if (mod == 1) {
      decode.Vib_Count = (bytes[3] << 24 | bytes[4] << 16 | bytes[5] << 8 | bytes[6]) >>> 0;
      decode.Work_Min = (bytes[7] << 24 | bytes[8] << 16 | bytes[9] << 8 | bytes[10]) >>> 0;
    }
    else if (mod == 2) {
      decode.Vib_Count = (bytes[3] << 24 | bytes[4] << 16 | bytes[5] << 8 | bytes[6]) >>> 0;
      decode.TempC_SHT = parseFloat(((bytes[7] << 24 >> 16 | bytes[8]) / 100).toFixed(2));
      decode.Hum_SHT = parseFloat((((bytes[9] << 8 | bytes[10]) & 0xFFF) / 10).toFixed(1));
    }
    else if (mod == 3) {
      decode.TempC_SHT = parseFloat(((bytes[3] << 24 >> 16 | bytes[4]) / 100).toFixed(2));
      decode.Hum_SHT = parseFloat((((bytes[5] << 8 | bytes[6]) & 0xFFF) / 10).toFixed(1));
      decode.Work_Min = (bytes[7] << 24 | bytes[8] << 16 | bytes[9] << 8 | bytes[10]) >>> 0;
    }

    decode.Alarm = (bytes[2] & 0x01) ? 1 : 0;
    decode.TDC = (bytes[2] & 0x02) ? 1 : 0;

    return decode;
  }

  if (fPort == 7) {
    var bat_mv = bytes[0] << 8 | bytes[1];
    var data_sum;
    for (var k = 2; k < bytes.length; k = k + 6) {
      var data = datalog(k, bytes);
      if (k == 2)
        data_sum = data;
      else
        data_sum += data;
    }
    return {
      Bat_mV: bat_mv,
      DATALOG: data_sum
    };
  }

  if (fPort == 9) {
    decode.Bat_mV = bytes[0] << 8 | bytes[1];
    decode.Max_Acc_X = parseFloat(((bytes[2] << 24 >> 16 | bytes[3]) / 1000).toFixed(3));
    decode.Max_Acc_Y = parseFloat(((bytes[4] << 24 >> 16 | bytes[5]) / 1000).toFixed(3));
    decode.Max_Acc_Z = parseFloat(((bytes[6] << 24 >> 16 | bytes[7]) / 1000).toFixed(3));
    return decode;
  }

  if (fPort == 5) {
    decode.Sensor_Model = bytes[0];
    decode.Firmware_Version = str_pad((bytes[1] << 8) | bytes[2]);
    decode.Freq_Band = bytes[3];
    decode.Sub_Band = bytes[4];
    decode.Bat_mV = bytes[5] << 8 | bytes[6];
    return decode;
  }
}