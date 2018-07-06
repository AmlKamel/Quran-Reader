// MirrorRealitySample.cpp : Defines the entry point for the application.
//

#include "stdafx.h"
#include <math.h>
#include "MirrorRealitySDK.h"
#include "MirrorRealitySample.h"
#include <iostream>
#include <ole2.h>
#include <olectl.h>
#include<windowsx.h>

#define MAX_LOADSTRING 100

HINSTANCE hInst;
TCHAR szTitle[MAX_LOADSTRING];
TCHAR szWindowClass[MAX_LOADSTRING];

HWND hDlgForm;
HWND hDlgChilddForm;
HDC hDC;
HGLRC hRC;

RECT rcOwner;
RECT wPos;
int ScreenX, ScreenY;

bool Exit;

#define MAX_FACES 8

const int mask_count = 15;
const char * masks[mask_count] = {
    "img\\color1",
	"img\\color2",
    "img\\color3",
    "img\\color4",
    "img\\color5",
    "img\\color6",
    "img\\color7",
    "img\\color8",
    "img\\color9",
    "img\\color10",
    "img\\color11",
    "img\\color12",
    "img\\color13",
    "img\\color14",
    "img\\color15",
};
 char * masksData[mask_count] = {
	"data\\img1.png",
	"data\\img2.png",
	"data\\img3.png",
	"data\\img4.png",
	"data\\img5.png",
	"data\\img6.png",
	"data\\img7.png",
	"data\\img8.png",
	"data\\img9.png",
	"data\\img10.png",
	"data\\img11.png",
	"data\\img12.png",
	"data\\img13.png",
	"data\\img14.png",
	"data\\img15.png",
};
int shifts[mask_count] =    {
    MR_SHIFT_TYPE_NO,
    MR_SHIFT_TYPE_NO,
    MR_SHIFT_TYPE_NO,
    MR_SHIFT_TYPE_NO,
    MR_SHIFT_TYPE_NO,
    MR_SHIFT_TYPE_NO,
    MR_SHIFT_TYPE_NO,
    MR_SHIFT_TYPE_NO,
    MR_SHIFT_TYPE_NO,
    MR_SHIFT_TYPE_NO,
    MR_SHIFT_TYPE_NO,
    MR_SHIFT_TYPE_NO,
    MR_SHIFT_TYPE_NO,
    MR_SHIFT_TYPE_IN,  // young3
    MR_SHIFT_TYPE_NO,
};

int mask_number = 0;

GLuint maskTexture1;
GLuint maskTexture2;
int isMaskTexture1Created = 0;
int isMaskTexture2Created = 0;
MR_MaskFeatures maskCoords;

// Forward declarations of functions included in this code module:
BOOL				InitInstance(HINSTANCE, int);
INT_PTR CALLBACK	CamFaceInterface(HWND, UINT, WPARAM, LPARAM);
INT_PTR CALLBACK	DialogInterface(HWND, UINT, WPARAM, LPARAM);

bool FileExists(LPCSTR fname)
{
  return ::GetFileAttributesA(fname) != DWORD(-1);
}

GLvoid InitGL()
{
	glClearColor(0.0f, 0.0f, 0.0f, 0.0f);
	glClearDepth(1.0);
	glDepthFunc(GL_LESS);
	glEnable(GL_DEPTH_TEST);
	glMatrixMode(GL_PROJECTION);
	glLoadIdentity();
	glMatrixMode(GL_MODELVIEW);
}

void LoadMask()
{
	HImage img;
	HImage normal_img;

	if (isMaskTexture1Created) {
		glDeleteTextures(1, &maskTexture1);
	}
	if (isMaskTexture2Created) {
		glDeleteTextures(1, &maskTexture2);
	}
	isMaskTexture1Created = 0;
	isMaskTexture2Created = 0;

	const char * effectname = masks[mask_number];
	
	char grdname[1024];
	strcpy_s(grdname, 1024, effectname);
	strcpy_s(grdname + strlen(grdname), 1024 - strlen(grdname), ".grd");
	char topname[1024];
	strcpy_s(topname, 1024, effectname);
	strcpy_s(topname + strlen(topname), 1024 - strlen(topname), "_normal.png");
	char maskname[1024];
	strcpy_s(maskname, 1024, effectname);
	strcpy_s(maskname + strlen(maskname), 1024 - strlen(maskname), ".png");
	
	if (FSDK_LoadImageFromFileWithAlpha(&img, maskname) != FSDKE_OK) {
		FSDK_CreateEmptyImage(&img);
	}
	if (FSDK_LoadImageFromFileWithAlpha(&normal_img, topname) != FSDKE_OK) {
		FSDK_CreateEmptyImage(&normal_img);
	}

	int result = MR_LoadMaskCoordsFromFile(grdname, maskCoords);
	if (result == FSDKE_OK) {
		glGenTextures(1, &maskTexture1);
		glGenTextures(1, &maskTexture2);
		MR_LoadMask(img, normal_img, maskTexture1, maskTexture2, &isMaskTexture1Created, &isMaskTexture2Created);
	}

	FSDK_FreeImage(img);
	FSDK_FreeImage(normal_img);
}

void LoadNextMask()
{
	mask_number = (mask_number+1) % mask_count;
    LoadMask();
}

int APIENTRY _tWinMain(HINSTANCE hInstance,
                     HINSTANCE hPrevInstance,
                     LPTSTR    lpCmdLine,
                     int       nCmdShow)
{
	UNREFERENCED_PARAMETER(hPrevInstance);
	UNREFERENCED_PARAMETER(lpCmdLine);

	LoadString(hInstance, IDS_APP_TITLE, szTitle, MAX_LOADSTRING);
	LoadString(hInstance, IDC_MIRRORREALITY, szWindowClass, MAX_LOADSTRING);
	

	wchar_t filename[_MAX_PATH];
    GetModuleFileNameW(NULL, filename, _MAX_PATH);
	int i, sl;
	sl=-1;
	#if defined( _WIN32 ) || defined ( _WIN64 )
		wchar_t slash=L'\\';
	#else
		wchar_t slash=L'/';
	#endif
	for (i=0; filename[i]!=L'\0';i++)
	{
		if (filename[i]==slash)
			sl=i;
	}
	filename[sl+1]=L'\0';

	SetCurrentDirectoryW(filename);
	//forRegister = MyRegisterClass(hInstance);
	// Perform application initialization:
	return InitInstance(hInstance, nCmdShow);
}

void ReSizeGLScene(GLsizei Width, GLsizei Height) {
	if (Height == 0) Height=1;
	glViewport(0, 0, Width, Height);
	glMatrixMode(GL_PROJECTION);
	glLoadIdentity();
	glMatrixMode(GL_MODELVIEW);
}

void LoadGLTextures(int w, int h, GLuint * texture, HImage image) { 
	int Width, Height;
	FSDK_GetImageWidth(image, &Width);
	FSDK_GetImageHeight(image, &Height);

	unsigned char * data;
	data = new unsigned char[4 * Width * Height];
	FSDK_SaveImageToBuffer(image, data, FSDK_IMAGE_COLOR_32BIT);
	
	for (int x = 0; x < 4 * Width; x++)
		for (int y = 0; y < Height / 2; y++) {
			unsigned char t;
			t = data[x + y * 4 * Width];
			data[x + y * Width * 4] = data[x + (Height - y - 1) * Width * 4];
			data[x + (Height - y - 1) * Width * 4] = t;
		}

	glGenTextures(1, texture);
    glBindTexture(GL_TEXTURE_2D, *texture);
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
	glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA, Width, Height, 0, GL_RGBA, GL_UNSIGNED_BYTE, data);
	
	delete [] data;
}
HWND hOwner;
BOOL InitInstance(HINSTANCE hInstance, int nCmdShow)
{
	hInst = hInstance; // Store instance handle in our global variable
	char ch[] = "RkanvCCmO26GNNEICaIcCSr+b2OhOiH1gAq0xOkEaSDuPOk25L91RqF5WnWtBFmHyyJn8lX6yHgjvl2GWu3yuOXFhavBm8+tUZuRYzCCXIuXqA5mt9GuyjvIee9MPuAM15rGFKdwEmaDy1GERYnWXnfqRueWB8fW5qngrWSH45h=";
	int res = MR_ActivateLibrary(ch);
	if (FSDKE_OK != res){
		MessageBox(NULL, L"MirrorReality SDK is not activated.", L"Error", MB_ICONERROR | MB_OK);
		return FALSE;
	}
	FSDK_Initialize("");
	FSDK_InitializeCapturing();

	int CameraCount = 0;
	wchar_t ** CameraList;
	FSDK_GetCameraList(&CameraList, &CameraCount);
	if (0 == CameraCount)
	{
		MessageBox(0, L"Please attach a camera", L"Error", MB_ICONERROR | MB_OK);
		return FALSE;
	}

	int VideoFormatCount = 0;
	FSDK_VideoFormatInfo * VideoFormatList;
	FSDK_GetVideoFormatList(CameraList[0], &VideoFormatList, &VideoFormatCount);
	FSDK_VideoFormatInfo videoFormat = VideoFormatList[0];
	FSDK_SetVideoFormat(CameraList[0], videoFormat);
	ScreenX = videoFormat.Width;
	ScreenY = videoFormat.Height;

    int handle;
	if (FSDKE_OK != FSDK_OpenVideoCamera(CameraList[0], &handle))
	{
		MessageBox(0, L"Error opening the camera", L"Error", MB_ICONERROR | MB_OK);
		return FALSE;
	}

	hOwner = GetDesktopWindow();
	GetWindowRect(hOwner, &rcOwner);

	hDlgChilddForm = CreateDialog(NULL, MAKEINTRESOURCE(IDD_CAMFACESDK), NULL, CamFaceInterface);
	hDlgForm = CreateDialog(NULL, MAKEINTRESOURCE(IDD_DIALOG1), NULL, DialogInterface);
	HICON hIcon1 = LoadIcon(GetModuleHandle(NULL), MAKEINTRESOURCE(IDI_MIRRORREALITY));
	SendMessage(hDlgForm, WM_SETICON, (WPARAM)ICON_BIG, (LPARAM)hIcon1);
	ShowWindow(hDlgForm, SW_SHOW);
	ShowWindow(hDlgChilddForm, SW_SHOW);
	SetParent(hDlgChilddForm, hDlgForm);
	UpdateWindow(hDlgForm);
	UpdateWindow(hDlgChilddForm);
	LoadMask();
	
	HTracker tracker;
	FSDK_CreateTracker(&tracker);
	int err = 0;
	FSDK_SetTrackerMultipleParameters(tracker, "RecognizeFaces=false; DetectFacialFeatures=true; HandleArbitraryRotations=false; DetermineFaceRotationAngle=false; InternalResizeWidth=100; FaceDetectionThreshold=5;", &err);
	
	Exit = false;
	while (!Exit)
	{
		HImage image;
	
		FSDK_GrabFrame(handle, &image);
	
		int detected = 0;
	
		long long IDs[256];
		long long count = 0;
		FSDK_FeedFrame(tracker, 0, image, &count, IDs, sizeof(IDs));
		int ww, hh;
		FSDK_GetImageWidth(image, &ww);
		FSDK_GetImageHeight(image, &hh);
		detected = (int)count;
		FSDK_Features f[MR_MAX_FACES];
		for (int facenum = 0; facenum < count; facenum++)
	    {
			FSDK_GetTrackerFacialFeatures(tracker, 0, IDs[facenum], &(f[facenum]));
			for (int i = 0; i < FEATURES_NUMBER; ++i)
	        {
				f[facenum][i].x = f[facenum][i].x * ScreenX / ww;
				f[facenum][i].y = f[facenum][i].y * ScreenY / hh;
			}
		}
	
		ReSizeGLScene(ScreenX, ScreenY);
	
		GLuint texture;
		LoadGLTextures(ScreenX, ScreenY, &texture, image);
	
		MR_DrawGLScene(texture, detected, f, 0, shifts[mask_number], maskTexture1, maskTexture2, maskCoords, isMaskTexture1Created, isMaskTexture2Created, ScreenX, ScreenY);
	
		SwapBuffers(hDC);
	
		glDeleteTextures(1, &texture);
	
		FSDK_FreeImage(image);

	    MSG msg;
	    if (PeekMessage(&msg, NULL, 0, 0, PM_REMOVE))
		{
		   if (!IsWindow(hDlgForm) || !IsDialogMessage(hDlgForm, &msg))
		   {
			TranslateMessage(&msg);
			DispatchMessage(&msg);
		   }
	    }
    }

    FSDK_FreeTracker(tracker);

    if (FSDK_CloseVideoCamera(handle) < 0) 
	{
	  MessageBox(0, TEXT("Error closing camera."), TEXT("Error"), MB_ICONERROR | MB_OK);
    }

	FSDK_FreeVideoFormatList(VideoFormatList);
	FSDK_FreeCameraList(CameraList, CameraCount);

	FSDK_FinalizeCapturing();
	FSDK_Finalize();

	return TRUE;
}

int ImageSize = 60;
int colCount = 8;
int rowCount = (mask_count % colCount == 0) ? mask_count / colCount : mask_count / colCount + 1;
int rectRightTopX = ImageSize * colCount;
int rectRightTopY;
int buttonSize = 40;
HBRUSH hb;
int btnXPos;
int btnCamYPos;
int btnFavYPos;
int btnToCardYPos;
bool maincap()
{
	int x1, y1, x2, y2, w, h;
	RECT rc;
	GetWindowRect(hDlgChilddForm, &rc);
	x1 = rc.left;
	y1 = rc.top;
	x2 = rc.right-10;
	y2 = rc.bottom;
	w = x2 - x1;
	h = y2 - y1;
	HDC hdcSource = GetDC(NULL);
	HDC hdcMemory = CreateCompatibleDC(hdcSource);
	HBITMAP hBitmap = CreateCompatibleBitmap(hdcSource, w, h);
	HBITMAP hBitmapOld = (HBITMAP)SelectObject(hdcMemory, hBitmap);

	BitBlt(hdcMemory, 0, 0, w, h, hdcSource, x1, y1, SRCCOPY);
	hBitmap = (HBITMAP)SelectObject(hdcMemory, hBitmapOld);

	DeleteDC(hdcSource);
	DeleteDC(hdcMemory);
	HImage im;
	FSDK_LoadImageFromHBitmap(&im, hBitmap);
	if (FSDK_SaveImageToFile(im, "data11.png"))return true;
	return false;
}

INT_PTR CALLBACK CamFaceInterface(HWND hDlg, UINT message, WPARAM wParam, LPARAM lParam)
{
	UNREFERENCED_PARAMETER(lParam);

	GLuint	PixelFormat;
	static	PIXELFORMATDESCRIPTOR pfd=
	{
		sizeof(PIXELFORMATDESCRIPTOR),
		1,
		PFD_DRAW_TO_WINDOW |
		PFD_SUPPORT_OPENGL |
		PFD_DOUBLEBUFFER,
		PFD_TYPE_RGBA,
		16,
		0, 0, 0, 0, 0, 0,
		0,
		0,
		0,
		0, 0, 0, 0,
		16,  
		0,
		0,
		PFD_MAIN_PLANE,
		0,
		0, 0, 0
	};

	switch (message)
	{

	case WM_INITDIALOG:
		hDC = GetDC(hDlg);
		PixelFormat = ChoosePixelFormat(hDC, &pfd);
		if (!PixelFormat)
		{
			MessageBox(0,L"Can't Find A Suitable PixelFormat.",L"Error",MB_OK|MB_ICONERROR);
			PostQuitMessage(0);
			break;
		}
		if (!SetPixelFormat(hDC,PixelFormat,&pfd))
		{
			MessageBox(0,L"Can't Set The PixelFormat.",L"Error",MB_OK|MB_ICONERROR);
			PostQuitMessage(0);
			break;
		}
		hRC = wglCreateContext(hDC);
		if(!hRC)
		{
			MessageBox(0,L"Can't Create A GL Rendering Context.",L"Error",MB_OK|MB_ICONERROR);
			PostQuitMessage(0);
			break;
		}
		if(!wglMakeCurrent(hDC, hRC))
		{
			MessageBox(0,L"Can't activate GLRC.",L"Error",MB_OK|MB_ICONERROR);
			PostQuitMessage(0);
			break;
		}
		InitGL();

		/*newScreenX = (rcOwner.left + rcOwner.right)/2.0f;
		newScreenY = (rcOwner.top + rcOwner.bottom)/2.0f;
		if (newScreenX/ScreenX <= newScreenY/ScreenY) {
			ScreenY = (int)(ScreenY*newScreenX/ScreenX);
			ScreenX = (int)newScreenX;
		} else {
			ScreenX = (int)(ScreenX*newScreenY/ScreenY);
			ScreenY = (int)newScreenY;
		}*/

		//SetWindowLong(hDlg, WS_CHILD,WS_CAPTION | WS_SYSMENU | WS_MINIMIZEBOX);
		SetWindowPos(hDlg, NULL, /*(rcOwner.left + rcOwner.right)/4*/0, /*(rcOwner.top + rcOwner.bottom)/4*/0,
			ScreenX, ScreenY,/*For help message*/NULL);
		GetClientRect(hDlg, &wPos);
		
	    return (INT_PTR)TRUE;


	case WM_SIZE:
		return (INT_PTR)TRUE;

	case WM_RBUTTONDOWN:
		LoadNextMask();
		break;

	case WM_LBUTTONDOWN:
		maincap();
		break;

	case WM_COMMAND:
		if (LOWORD(wParam) == IDCANCEL)
		{
			EndDialog(hDlg, LOWORD(wParam));
			Exit = true;
		}
	}

	return (INT_PTR)FALSE;
}

void DrawToRect(HDC hdc,RECT rc,char*path,int size)
{
	HImage img;
	//const char* ch = masksData[i];
	HBITMAP hbitmap = NULL;
	FSDK_LoadImageFromFileWithAlpha(&img, path);
	FSDK_SaveImageToHBitmap(img, &hbitmap);
	BITMAP bitmap;

	HGDIOBJ oldbitmap;
	HDC hdcmem;
	hdcmem = CreateCompatibleDC(hdc);

	oldbitmap = SelectObject(hdcmem, hbitmap);

	GetObject(hbitmap, sizeof(BITMAP), &bitmap);
	BitBlt(hdc, rc.left, rc.top, size, size, hdcmem, 0, 0, SRCCOPY);
	SelectObject(hdcmem, oldbitmap);
	DeleteDC(hdcmem);
}

//x,y for point int most right top
bool GetImagesBoard(HWND whnd, RECT* pRect,int RTx,int RTy,int rows)
{
	RECT rc;
	if (GetClientRect(whnd, &rc))
	{
		pRect->left = 0;
		pRect->top = RTy;
		pRect->right = RTx;
		pRect->bottom = pRect->top + ImageSize*rows ;
		return true;
	}
	SetRectEmpty(pRect);
	return false;
}
void DrawLine(HDC hdc,int x1, int y1, int x2, int y2)
{
	MoveToEx(hdc,x1, y1, NULL);
	LineTo(hdc, x2, y2);
}
int GetCellNumberFromPoint(HWND hDlg ,int x,int y, int RTx, int RTy, int rows)
{
	RECT rc;
	POINT pt = {x, y};
	if (GetImagesBoard(hDlg, &rc, RTx, RTy, rows))
	{
		if (PtInRect(&rc, pt))
		{
			//point inside the rect
			//pt.x from (0 to RTx)  ,dictance from pt.x and left=pt.x
			//pt.y from (RTy to rows*ImageSize)  ,dictance from pt.y and top=pt.y-RTy
			y = pt.y - RTy;
			int row = y / ImageSize;
			int col = x / ImageSize;
			return row * colCount + col;
		}
	}
	return -1;
}
bool GetCellRect(HWND hDlg, int index, RECT *rc)
{
	RECT imagesRec;
	if (GetImagesBoard(hDlg,&imagesRec, rectRightTopX, rectRightTopY,rowCount))
	{
		int rowIndex = index / colCount;
		int colIndex = index % colCount;
		rc->top = imagesRec.top + rowIndex * ImageSize;
		rc->bottom = rc->top+ ImageSize;
		rc->left = imagesRec.left + colIndex * ImageSize;
		rc->right = rc->left+ ImageSize;
		return true;
	}
	
	return false;
}
bool GetButton(HWND whnd, RECT* pRect,int x,int y)
{
	RECT rc;
	if (GetClientRect(whnd, &rc))
	{
		pRect->left = x;
		pRect->top = y;
		pRect->right = pRect->left + buttonSize;
		pRect->bottom = pRect->top + buttonSize;
		return true;
	}
	SetRectEmpty(pRect);
	return false;
}

INT_PTR CALLBACK DialogInterface(HWND hDlg, UINT message, WPARAM wParam, LPARAM lParam)
{
	rectRightTopY = ScreenY;
	switch (message)
	{
		
	 case WM_INITDIALOG:
		{
		 SetWindowLong(hDlg, WS_MINIMIZEBOX | WS_MAXIMIZEBOX, WS_CAPTION | WS_MINIMIZEBOX);
		 SetWindowPos(hDlg, NULL, /*(rcOwner.left + rcOwner.right)/4*/100, /*(rcOwner.top + rcOwner.bottom)/4*/0,
			ScreenX, ScreenY + 200,/*For help message*/NULL);
		 GetClientRect(hDlg, &wPos);
		 return (INT_PTR)TRUE;
		}
		
	 case WM_PAINT:
	    {
		 PAINTSTRUCT ps;
		 HDC hdc = BeginPaint(hDlg, &ps);
		 RECT rc;
		 if (GetImagesBoard(hDlg, &rc, rectRightTopX, rectRightTopY,rowCount))
		 {
			Rectangle(hdc, rc.left, rc.top, rc.right, rc.bottom);
			//draw vertical lines
			for (int i = 1; i < colCount; i++)
				DrawLine(hdc, rc.left + i * ImageSize, rc.top, rc.left + i * ImageSize, rc.bottom);
			//draw horizontal lines
			for (int i = 1; i < rowCount; i++)
				DrawLine(hdc, 0, rc.top + i * ImageSize, rc.right , rc.top + i * ImageSize);
			if (NULL != hdc)
			{
				for (int i = 0; i < rowCount*colCount; i++)
				{
					//get cell rec to draw on it
					 RECT cellRc;
					 if (i < mask_count)
					 {
						 if (GetCellRect(hDlg, i, &cellRc))
							 DrawToRect(hdc, cellRc, masksData[i], ImageSize);
					 }
					 else 
						 if (GetCellRect(hDlg, i, &cellRc))
							 DrawToRect(hdc, cellRc, "OriginalBackground.jpg", ImageSize);
				}
			}
		 }
		// TODO: Add any drawing code that uses hdc here...
		 //draw buttons
		  btnXPos = rectRightTopX+20;
		  btnCamYPos = ScreenY + 5;
		  btnFavYPos = btnCamYPos + buttonSize + 10;
		 btnToCardYPos= btnFavYPos+ buttonSize + 10;
		 //camera buttons
		 if (GetButton(hDlg, &rc, btnXPos,btnCamYPos))
			 DrawToRect(hdc, rc, "camera.png",buttonSize);
		 //favourite button
		 if (GetButton(hDlg, &rc, btnXPos, btnFavYPos))
			 DrawToRect(hdc, rc, "heart3.png", buttonSize);
		 //add to card button
		 if (GetButton(hDlg, &rc, btnXPos, btnToCardYPos))
			 DrawToRect(hdc, rc, "card3.png", buttonSize);
		EndPaint(hDlg, &ps);
	    }
	 break;
	 case WM_LBUTTONDOWN:
		{
		 int x = GET_X_LPARAM(lParam);
		 int y = GET_Y_LPARAM(lParam);
		 int index = GetCellNumberFromPoint(hDlg,x,y, rectRightTopX, rectRightTopY,rowCount);
		 if (index != -1 &&index <mask_count)
		 {
			 mask_number = index;
			 LoadMask();
		 }
		 else
		 {
			 RECT rc;
			 POINT pt = { x, y };
			 if (GetButton(hDlg, &rc, btnXPos, btnCamYPos))
			 {
				 if (PtInRect(&rc, pt))
					 maincap();
			 }
			 //else if (GetButton(hDlg, &rc, FavXPos, FavYPos))
			 //{
				// if (PtInRect(&rc, pt))
				//	 //add to favorite code

			 //}
			 //else if (GetButton(hDlg, &rc, FavXPos, FavYPos))
			 //{
				// if (PtInRect(&rc, pt))
				// //add to card code

			 //}
		 }
		}
		break;
	case WM_SIZE:
		return (INT_PTR)TRUE;
	case WM_COMMAND:
		if (LOWORD(wParam) == IDCANCEL)
		{
			EndDialog(hDlg, LOWORD(wParam));
			Exit = true;
		}
	case WM_DESTROY:
		{
		DeleteObject(hb);
		}
		break;
    }

 return (INT_PTR)FALSE;
}

