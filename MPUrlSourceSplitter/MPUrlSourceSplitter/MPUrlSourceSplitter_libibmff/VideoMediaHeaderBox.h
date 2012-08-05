/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#pragma once

#ifndef __VIDEO_MEDIA_HEADER_BOX_DEFINED
#define __VIDEO_MEDIA_HEADER_BOX_DEFINED

#include "FullBox.h"

#define VIDEO_MEDIA_HEADER_BOX_TYPE                                           L"vmhd"

class CVideoMediaHeaderBox :
  public CFullBox
{
public:
  // initializes a new instance of CVideoMediaHeaderBox class
  CVideoMediaHeaderBox(void);

  // destructor
  virtual ~CVideoMediaHeaderBox(void);

  /* get methods */

  // gets whole box into buffer
  // @param buffer : the buffer for box data
  // @param length : the length of buffer for data
  // @return : true if all data were successfully stored into buffer, false otherwise
  virtual bool GetBox(uint8_t **buffer, uint32_t *length);

  // gets composition mode for this video track
  // @return : composition mode for this video track
  virtual uint16_t GetGraphicsMode(void);

  virtual uint16_t GetColorRed(void);

  virtual uint16_t GetColorGreen(void);

  virtual uint16_t GetColorBlue(void);

  /* set methods */

  /* other methods */

  // parses data in buffer
  // @param buffer : buffer with box data for parsing
  // @param length : the length of data in buffer
  // @return : true if parsed successfully, false otherwise
  virtual bool Parse(const uint8_t *buffer, uint32_t length);

  // gets box data in human readable format
  // @param indent : string to insert before each line
  // @return : box data in human readable format or NULL if error
  virtual wchar_t *GetParsedHumanReadable(const wchar_t *indent);

protected:

  // composition mode for this video track, from the following enumerated set, which may be extended by derived specifications:
  // copy = 0 copy over the existing image
  uint16_t graphicsMode;

  uint16_t colorRed;

  uint16_t colorGreen;

  uint16_t colorBlue;

  // gets box size added to size
  // method is called to determine whole box size for storing box into buffer
  // @param size : the size of box calling this method
  // @return : size of box 
  virtual uint64_t GetBoxSize(uint64_t size);

  // parses data in buffer
  // @param buffer : buffer with box data for parsing
  // @param length : the length of data in buffer
  // @param processAdditionalBoxes : specifies if additional boxes have to be processed
  // @return : true if parsed successfully, false otherwise
  virtual bool ParseInternal(const unsigned char *buffer, uint32_t length, bool processAdditionalBoxes);
};

#endif