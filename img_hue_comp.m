% Load LSL library
lib = lsl_loadlib();

% Create outlet
info = lsl_streaminfo(lib,'shirt_in','Markers',1,0,'cf_string','001');
outlet = lsl_outlet(info);

% Load inlet
result = {};
while isempty(result)
    result = lsl_resolve_byprop(lib,'name','shirt_out'); 
end
inlet = lsl_inlet(result{1});
disp('inlet created');
mrks = '';
while true
    % Wait for filenames
    [mrks,ts] = inlet.pull_sample();
    if isempty(mrks); continue; end;
     input = mrks{1};
     disp(input);
    inputFiles = strsplit(input);
    img1 = imread(['C:\Users\TDA\Documents\Python\IBMRescueFace\imgs\' inputFiles{1}]);
    img2 = imread(['C:\Users\TDA\Documents\Python\IBMRescueFace\imgs\' inputFiles{2}]);
    im_hsv1 = rgb2hsv(img1);
    im_hsv2 = rgb2hsv(img2);
    [counts_img1,binLocations1] = imhist(im_hsv1(:,:,1),360);
    [counts_img2,binLocations2] = imhist(im_hsv2(:,:,1),360);
    bin1 = find(counts_img1 == max(counts_img1));
    bin2 = find(counts_img2 == max(counts_img2));

    threshold = 10;
    if bin1 >= 360 - threshold
        bin1 = bin1 - 360;
    end

    if bin2 >= 360 - threshold
       bin2 = bin2 - 360; 
    end

    match = false;
    if abs(bin1-bin2) <= threshold
        match = true;
    end

    % Return match to LSL
    disp(num2str(match));
    outlet.push_sample({num2str(match)});
    %break
end


