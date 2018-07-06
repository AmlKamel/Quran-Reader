///////////////////////////////////////////////////
//
//         Luxand MirrorReality Library
//
//
//  Copyright(c) 2005-2016 Luxand, Inc.
//         http://www.luxand.com
//
///////////////////////////////////////////////////

#ifndef _LUXAND_MIRROR_REALITY_SDK_
#define _LUXAND_MIRROR_REALITY_SDK_

#if defined( _WIN32 ) || defined ( _WIN64 )
	#define _MRSDKIMPORT_ __declspec(dllimport) __cdecl
    #include <windows.h>
#else
	#define _MRSDKIMPORT_
	#define __cdecl
#endif

#if defined( _WIN32 ) || defined ( _WIN64 )
#include <gl/gl.h>
#include <gl/glu.h>
#else
#ifdef __ANDROID__
#include <EGL/egl.h>
#include <GLES/gl.h>
#else //iOS
#import <OpenGLES/ES1/gl.h>
#import <OpenGLES/ES1/glext.h>
#endif
#endif

#include "LuxandFaceSDK.h"

#define MR_MAX_FACES 5

typedef struct {
    float x, y;
} TPointf;

typedef TPointf MR_MaskFeatures [FEATURES_NUMBER];

const int MR_MASK_TEXTURE_SIZE = 1024;

const int MR_SHIFT_TYPE_NO = 0;
const int MR_SHIFT_TYPE_OUT = 1;
const int MR_SHIFT_TYPE_IN = 2;
const int MR_SHIFT_TYPE_OPERATION = 1000;

int _MRSDKIMPORT_ MR_ActivateLibrary(char * LicenseKey);

int _MRSDKIMPORT_ MR_LoadMaskCoordsFromFile(const char * filename, MR_MaskFeatures maskCoords);

int _MRSDKIMPORT_ MR_LoadMask(HImage maskImage1, HImage maskImage2, GLuint maskTexture1, GLuint maskTexture2, int * isTexture1Created, int * isTexture2Created);

int _MRSDKIMPORT_ MR_DrawGLScene(GLuint facesTexture, int facesDetected, FSDK_Features features[MR_MAX_FACES], int rotationAngle90Multiplier, int shiftType, GLuint maskTexture1, GLuint maskTexture2, MR_MaskFeatures maskCoords, int isTexture1Created, int isTexture2Created, int width, int height);


#endif
