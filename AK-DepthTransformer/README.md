# Azure Kinect DK DepthtoColor Transform Offline

# File Path
- Depth Image
  - AzureKinectData <yymmdd> --> <subject_id> --> <record_sequence> --> <2_DepthImage> --> <kinect_location> --> *.png
- Calibration JSON
   - AzureKinectData <yymmdd> --> <subject_id> --> <record_sequence> --> <6_CameraInfo> --> *.json

# Requirement
  - Conected Any Azure Kinect to PC
 
# Work Flow
  1. Get Depth Image & Calibration JSON
  2. Open Any Azure Kinect and Get Calibration Information
  3. Change Calibtaion Information to Depth Image Captured Azure Kinect Camera
  4. Transformation Depth to Color Image
  5. Save Transformed Depth Image
