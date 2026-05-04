function decodeUplink(input) {
  return {
    data: Decode(input.fPort, input.bytes, input.variables)
  };
}

function Str1(str2) {
  var str3 = "";
  for (var i = 0; i < str2.length; i++) {
    if (str2[i] <= 0x0f) {
      str2[i] = "0" + str2[i].toString(16) + "";
    }
    str3 += str2[i].toString(16) + "";
  }
  return str3;
}

function str_pad(byte) {
  var zero = '00';
  var hex = byte.toString(16);
  var tmp = 2 - hex.length;
  return zero.substr(0, tmp) + hex + " ";
}

function datalog(i, bytes) {
  var Ext = bytes[6] & 0x0F;
  var bb;

  if (Ext == '5') {
    bb = bytes[0 + i] << 8 | bytes[1 + i];
  } else {
    bb = 0;  // No external sensor
  }

  var cc = parseFloat(((bytes[2 + i] << 24 >> 16 | bytes[3 + i]) / 100).toFixed(2));
  var dd = parseFloat((((bytes[4 + i] << 8 | bytes[5 + i]) & 0xFFF) / 10).toFixed(1));
  var ee = getMyDate((bytes[7 + i] << 24 | bytes[8 + i] << 16 | bytes[9 + i] << 8 | bytes[10 + i]).toString(10));
  var string = '[' + bb + ',' + cc + ',' + dd + ',' + ee + ']' + ',';

  return string;
}

function getzf(c_num) {
  if (parseInt(c_num) < 10)
    c_num = '0' + c_num;
  return c_num;
}

function getMyDate(str) {
  var c_Date;
  if (str > 9999999999)
    c_Date = new Date(parseInt(str));
  else
    c_Date = new Date(parseInt(str) * 1000);

  var c_Year = c_Date.getFullYear(),
    c_Month = c_Date.getMonth() + 1,
    c_Day = c_Date.getDate(),
    c_Hour = c_Date.getHours(),
    c_Min = c_Date.getMinutes(),
    c_Sen = c_Date.getSeconds();
  var c_Time = c_Year + '-' + getzf(c_Month) + '-' + getzf(c_Day) + ' ' + getzf(c_Hour) + ':' + getzf(c_Min) + ':' + getzf(c_Sen);

  return c_Time;
}

function Decode(fPort, bytes, variables) {
  var Ext = bytes[6] & 0x0F;
  var button = bytes[6] & 0x10;
  var poll_message_status = ((bytes[6] >> 6) & 0x03);

  var Connect = (bytes[6] & 0x80) >> 7;
  var decode = {};
  var data = {};

  if ((fPort == 3) && ((bytes[2] == 0x01) || (bytes[2] == 0x02) || (bytes[2] == 0x03) || (bytes[2] == 0x04))) {
    var array1 = [];
    var bytes1 = "0x";
    var str1 = Str1(bytes);
    var str2 = str1.substring(0, 6);
    var str3 = str1.substring(6,);
    var reg = /.{4}/g;
    var rs = str3.match(reg);
    rs.push(str3.substring(rs.join('').length));
    rs.pop();
    var new_arr = [...rs];
    var data1 = new_arr;
    decode.Bat_mV = parseInt(bytes1 + str2.substring(0, 4) & 0x3FFF);

    if (parseInt(bytes1 + str2.substring(4,)) == 1) {
      decode.sensor = "ds18b20";
    } else if (parseInt(bytes1 + str2.substring(4,)) == 2) {
      decode.sensor = "tmp117";
    } else if (parseInt(bytes1 + str2.substring(4,)) == 3) {
      decode.sensor = "gxht30";
    } else if (parseInt(bytes1 + str2.substring(4,)) == 4) {
      decode.sensor = "sht31";
    }

    for (var i = 0; i < data1.length; i++) {
      var temp = (parseInt(bytes1 + data1[i].substring(0, 4))) / 100;
      array1[i] = temp;
    }
    decode.Temp = array1;
    return decode;
  }
  else if (fPort == 5) {
    // Return raw byte values to match LHT52 format
    var firm_ver = (bytes[1] & 0x0f) + '.' + (bytes[2] >> 4 & 0x0f) + '.' + (bytes[2] & 0x0f);
    var bat = bytes[5] << 8 | bytes[6];

    return {
      Sensor_Model: bytes[0],
      Firmware_Version: firm_ver,
      Freq_Band: bytes[3],
      Sub_Band: bytes[4],
      Bat_mV: bat,
    };
  }

  switch (poll_message_status) {
    case 0: {
      decode.Bat_mV = ((bytes[0] << 8 | bytes[1]) & 0x3FFF);

      if (Ext != 0x0f) {
        decode.TempC_SHT = parseFloat(((bytes[2] << 24 >> 16 | bytes[3]) / 100).toFixed(2));
        decode.Hum_SHT = parseFloat((((bytes[4] << 8 | bytes[5]) & 0xFFF) / 10).toFixed(1));
      }

      if (Ext == '5') {
        decode.ILL_lx = bytes[7] << 8 | bytes[8];
      }
      if ((bytes.length == 11) || (bytes.length == 15)) {
        return decode;
      }
      break;
    }

    case 1: {
      for (var i = 0; i < bytes.length; i = i + 11) {
        var da = datalog(i, bytes);
        if (i == '0')
          decode.DATALOG = da;
        else
          decode.DATALOG += da;
      }
      return decode;
    }

    case 2: {
      for (var i = 0; i < bytes.length; i = i + 11) {
        var da = datalog(i, bytes);
        if (i == '0')
          data.DATALOG = da;
        else
          data.DATALOG += da;
      }
      return {
        data: data,
        retransmission_Status: "retransmission_Status",
      };
    }

    default:
      return {
        errors: ["unknown"]
      };
  }
}